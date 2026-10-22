namespace FinancialTracker.Application.Accounts;

public sealed class ListAccounts(IAccountStore accounts, TimeProvider clock)
{
    public async Task<IReadOnlyList<AccountSummary>> ExecuteAsync(Guid userId)
    {
        var today = clock.Today();
        return (await accounts.ListAsync(userId)).Select(account => AccountSummary.Of(account, today)).ToList();
    }
}
