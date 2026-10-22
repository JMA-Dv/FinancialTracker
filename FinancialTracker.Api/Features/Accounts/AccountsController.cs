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
    IValidator<CreateAccountRequest> createValidator,
    IValidator<RenameAccountRequest> renameValidator,
    IValidator<ChangeAccountCurrencyRequest> changeCurrencyValidator)
    : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<AccountResponse>> List() =>
        (await listAccounts.ExecuteAsync(User.Id())).Select(AccountResponse.Of).ToList();

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

public sealed record OpeningBalanceResponse(decimal Amount, DateOnly Date);

public sealed record AccountResponse(
    Guid Id,
    string Name,
    AccountKind Kind,
    string Currency,
    decimal Balance,
    bool IsLiability,
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
            account.OpeningBalance is { } opening ? new OpeningBalanceResponse(opening.Balance, opening.Date) : null);
}
