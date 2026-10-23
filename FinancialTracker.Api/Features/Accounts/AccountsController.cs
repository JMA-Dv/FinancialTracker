using FinancialTracker.Api.Features.Auth;
using FinancialTracker.Application.Accounts;
using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Money;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTracker.Api.Features.Accounts;

[ApiController]
[Route("api/accounts")]
public class AccountsController(
    OpenAccount openAccount,
    ListAccounts listAccounts,
    GetAccount getAccount,
    RenameAccount renameAccount,
    ChangeAccountCurrency changeAccountCurrency,
    CloseAccount closeAccount,
    ReopenAccount reopenAccount,
    GetAccountBalance getAccountBalance,
    GetAccountHistory getAccountHistory,
    IValidator<CreateAccountRequest> createValidator,
    IValidator<RenameAccountRequest> renameValidator,
    IValidator<ChangeAccountCurrencyRequest> changeCurrencyValidator)
    : ControllerBase
{
    /// <summary>The User's Accounts. Closed Accounts are left out unless asked for.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<AccountResponse>> List(bool includeClosed = false) =>
        (await listAccounts.ExecuteAsync(User.Id(), includeClosed)).Select(AccountResponse.Of).ToList();

    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Create(CreateAccountRequest request)
    {
        if (await InvalidAsync(createValidator, request) is { } invalid)
        {
            return invalid;
        }

        var account = await openAccount.ExecuteAsync(
            User.Id(), new OpenAccountRequest(
                request.Name,
                request.Kind ?? throw new InvalidOperationException("Kind is checked by the validator."),
                request.Currency,
                request.OpeningBalance is { } opening
                    ? new OpeningBalance(
                        opening.Amount ?? throw new InvalidOperationException("Amount is checked by the validator."),
                        opening.Date ?? throw new InvalidOperationException("Date is checked by the validator."))
                    : null));
        return CreatedAtAction(nameof(Get), new { id = account.Id }, AccountResponse.Of(account));
    }

    [HttpGet("{id:guid}")]
    public async Task<AccountResponse> Get(Guid id) =>
        AccountResponse.Of(await getAccount.ExecuteAsync(User.Id(), id));

    [HttpPut("{id:guid}/name")]
    public async Task<ActionResult<AccountResponse>> Rename(Guid id, RenameAccountRequest request)
    {
        if (await InvalidAsync(renameValidator, request) is { } invalid)
        {
            return invalid;
        }

        return AccountResponse.Of(await renameAccount.ExecuteAsync(User.Id(), id, request.Name));
    }

    /// <summary>Changes the Account's Currency. Refused with 409 once the Account has Transactions or Transfers.</summary>
    [HttpPut("{id:guid}/currency")]
    public async Task<ActionResult<AccountResponse>> ChangeCurrency(Guid id, ChangeAccountCurrencyRequest request)
    {
        if (await InvalidAsync(changeCurrencyValidator, request) is { } invalid)
        {
            return invalid;
        }

        return AccountResponse.Of(await changeAccountCurrency.ExecuteAsync(User.Id(), id, request.Currency));
    }

    /// <summary>Closes the Account, keeping its history. Refused with 409 unless its balance is zero.</summary>
    [HttpPost("{id:guid}/close")]
    public async Task<AccountResponse> Close(Guid id) => AccountResponse.Of(await closeAccount.ExecuteAsync(User.Id(), id));

    /// <summary>Opens a Closed Account again so money can be recorded against it.</summary>
    [HttpPost("{id:guid}/reopen")]
    public async Task<AccountResponse> Reopen(Guid id) => AccountResponse.Of(await reopenAccount.ExecuteAsync(User.Id(), id));

    /// <summary>What the Account held at the end of a date that isn't after today.</summary>
    [HttpGet("{id:guid}/balance")]
    public async Task<ActionResult<AccountBalanceResponse>> BalanceOn(Guid id, DateOnly? date)
    {
        if (date is not { } balanceDate)
        {
            ModelState.AddModelError(nameof(date), "A date is required.");
            return ValidationProblem(ModelState);
        }

        return new AccountBalanceResponse(balanceDate, await getAccountBalance.ExecuteAsync(User.Id(), id, balanceDate));
    }

    /// <summary>The Account's Transactions, Transfers and Balance Adjustments, newest first.</summary>
    [HttpGet("{id:guid}/history")]
    public async Task<IReadOnlyList<AccountHistoryEntryResponse>> History(Guid id) =>
        (await getAccountHistory.ExecuteAsync(User.Id(), id)).Select(AccountHistoryEntryResponse.Of).ToList();

    /// <summary>A 400 listing every broken rule, or null when the request is valid and can go on to the use case.</summary>
    private async Task<ActionResult?> InvalidAsync<T>(IValidator<T> validator, T request)
    {
        var result = await validator.ValidateAsync(request, HttpContext.RequestAborted);
        if (result.IsValid)
        {
            return null;
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return ValidationProblem(ModelState);
    }
}

internal sealed class RenameAccountRequestValidator : AbstractValidator<RenameAccountRequest>
{
    public RenameAccountRequestValidator() => RuleFor(r => r.Name).NotNull().MaximumLength(Account.MaxNameLength);
}

internal sealed class ChangeAccountCurrencyRequestValidator : AbstractValidator<ChangeAccountCurrencyRequest>
{
    public ChangeAccountCurrencyRequestValidator() => RuleFor(r => r.Currency).NotEmpty();
}

internal sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(r => r.Name).NotNull().MaximumLength(Account.MaxNameLength);
        RuleFor(r => r.Kind).NotNull().IsInEnum();
        RuleFor(r => r.Currency).NotEmpty().When(r => r.Currency is not null);
        // The `!` only silences FluentValidation's nullable annotations: the rule is skipped when there's no Opening Balance.
        RuleFor(r => r.OpeningBalance).SetValidator(new OpeningBalanceRequestValidator()!).When(r => r.OpeningBalance is not null);
    }
}

internal sealed class OpeningBalanceRequestValidator : AbstractValidator<OpeningBalanceRequest>
{
    public OpeningBalanceRequestValidator()
    {
        RuleFor(o => o.Amount).NotNull().InclusiveBetween(-Currency.MaxAmount, Currency.MaxAmount);
        RuleFor(o => o.Date).NotNull();
    }
}

public sealed record RenameAccountRequest(string Name);

/// <param name="Currency">An ISO 4217 code, such as USD.</param>
public sealed record ChangeAccountCurrencyRequest(string Currency);

/// <param name="Currency">An ISO 4217 code. Leave it out to use the User's Currency.</param>
/// <param name="OpeningBalance">Leave it out to start the Account at zero.</param>
public sealed record CreateAccountRequest(
    string Name, AccountKind? Kind, string? Currency, OpeningBalanceRequest? OpeningBalance);

/// <param name="Amount">The signed balance at the end of the date: negative is money owed.</param>
/// <param name="Date">The date tracking starts.</param>
public sealed record OpeningBalanceRequest(decimal? Amount, DateOnly? Date);

/// <summary>What the Account held at the end of <paramref name="Date"/>.</summary>
public sealed record AccountBalanceResponse(DateOnly Date, decimal Balance);

/// <param name="Id">The id of the Transaction, Transfer or Balance Adjustment, whichever the Kind says.</param>
/// <param name="Amount">How it changed the Account's balance, signed. For a Balance Adjustment, the difference it made.</param>
/// <param name="StatedBalance">The balance a Balance Adjustment states. Null for the others.</param>
public sealed record AccountHistoryEntryResponse(AccountHistoryKind Kind, Guid Id, DateOnly Date, decimal Amount, decimal? StatedBalance)
{
    public static AccountHistoryEntryResponse Of(AccountHistoryEntry entry) =>
        new(entry.Kind, entry.Id, entry.Date, entry.Amount, entry.StatedBalance);
}

public sealed record OpeningBalanceResponse(decimal Amount, DateOnly Date);

public sealed record AccountResponse(
    Guid Id,
    string Name,
    AccountKind Kind,
    string Currency,
    decimal Balance,
    bool IsLiability,
    bool IsClosed,
    OpeningBalanceResponse? OpeningBalance)
{
    public static AccountResponse Of(AccountSummary account) =>
        new(
            account.Id,
            account.Name,
            account.Kind,
            account.Currency.Code,
            account.Balance,
            account.IsLiability,
            account.IsClosed,
            account.OpeningBalance is { } opening ? new OpeningBalanceResponse(opening.Balance, opening.Date) : null);
}
