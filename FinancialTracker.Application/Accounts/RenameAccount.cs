namespace FinancialTracker.Application.Accounts;

public sealed class RenameAccount(IAccountStore accounts, TimeProvider clock)
{
    public async Task<AccountSummary> ExecuteAsync(Guid userId, Guid accountId, string name)
    {
        var account = await accounts.GetAsync(userId, accountId);
        account.Rename(name);
        await accounts.SaveChangesAsync();
        return AccountSummary.Of(account, clock.Today());
    }
}
