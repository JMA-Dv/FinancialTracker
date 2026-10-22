namespace FinancialTracker.Application.Accounts;

public sealed class GetAccount(IAccountStore accounts, TimeProvider clock)
{
    public async Task<AccountSummary> ExecuteAsync(Guid userId, Guid accountId) =>
        AccountSummary.Of(await accounts.GetAsync(userId, accountId), clock.Today());
}
