using FinancialTracker.Api.Features.Auth;
using FinancialTracker.Application.Accounts;
using FinancialTracker.Domain.Accounts;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTracker.Api.Features.Accounts;

[ApiController]
[Route("api/accounts")]
public class AccountsController(
    OpenAccount openAccount,
    ListAccounts listAccounts,
    GetAccount getAccount,
    RenameAccount renameAccount,
    ChangeAccountCurrency changeAccountCurrency)
    : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<AccountResponse>> List() =>
        (await listAccounts.ExecuteAsync(User.Id())).Select(AccountResponse.Of).ToList();

    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Create(CreateAccountRequest request)
    {
        var account = await openAccount.ExecuteAsync(
            User.Id(), new OpenAccountRequest(
                request.Name,
                request.Kind!.Value,
                request.Currency,
                request.OpeningBalance is { } opening ? new OpeningBalance(opening.Amount!.Value, opening.Date!.Value) : null));
        return CreatedAtAction(nameof(Get), new { id = account.Id }, AccountResponse.Of(account));
    }

    [HttpGet("{id:guid}")]
    public async Task<AccountResponse> Get(Guid id) =>
        AccountResponse.Of(await getAccount.ExecuteAsync(User.Id(), id));

    [HttpPut("{id:guid}/name")]
    public async Task<AccountResponse> Rename(Guid id, RenameAccountRequest request) =>
        AccountResponse.Of(await renameAccount.ExecuteAsync(User.Id(), id, request.Name));

    /// <summary>Changes the Account's Currency. Refused with 409 once the Account has Transactions or Transfers.</summary>
    [HttpPut("{id:guid}/currency")]
    public async Task<AccountResponse> ChangeCurrency(Guid id, ChangeAccountCurrencyRequest request) =>
        AccountResponse.Of(await changeAccountCurrency.ExecuteAsync(User.Id(), id, request.Currency));
}

public sealed record RenameAccountRequest(string Name);

/// <param name="Currency">An ISO 4217 code, such as USD.</param>
public sealed record ChangeAccountCurrencyRequest(string Currency);

/// <param name="Currency">An ISO 4217 code. Leave it out to use the User's Currency.</param>
/// <param name="OpeningBalance">Leave it out to start the Account at zero.</param>
public sealed record CreateAccountRequest(
    string Name, [Required] AccountKind? Kind, string? Currency, OpeningBalanceRequest? OpeningBalance);

/// <param name="Amount">The signed balance at the end of the date: negative is money owed.</param>
/// <param name="Date">The date tracking starts.</param>
public sealed record OpeningBalanceRequest([Required] decimal? Amount, [Required] DateOnly? Date);

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
