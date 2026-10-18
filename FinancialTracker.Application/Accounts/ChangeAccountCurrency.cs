using FinancialTracker.Domain.Money;

namespace FinancialTracker.Application.Accounts;

/// <summary>Changes an Account's Currency, which is only allowed before money moves through it (ADR 0003).</summary>
public sealed class ChangeAccountCurrency(IAccountStore accounts, TimeProvider clock)
{
    public async Task<AccountSummary> ExecuteAsync(Guid userId, Guid accountId, string currencyCode)
    {
        // Look the Account up first, so another User's Account is not found whatever the code.
        var account = await accounts.GetAsync(userId, accountId);
        var currency = Currency.FromCode(currencyCode);

        account.ChangeCurrency(currency, await accounts.HasTransactionsOrTransfersAsync(account.Id));
        await accounts.SaveChangesAsync();
        return AccountSummary.Of(account, clock.Today());
    }
}
