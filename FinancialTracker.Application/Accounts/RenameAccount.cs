using FinancialTracker.Application.Transactions;

namespace FinancialTracker.Application.Accounts;

public sealed class RenameAccount(IAccountStore accounts, ITransactionStore transactions, TimeProvider clock)
{
    public async Task<AccountSummary> ExecuteAsync(Guid userId, Guid accountId, string name)
    {
        var account = await accounts.GetAsync(userId, accountId);
        account.Rename(name);
        await accounts.SaveChangesAsync();
        return AccountSummary.Of(account, await transactions.SearchAsync(userId, TransactionFilter.ForAccount(account.Id)), clock.Today());
    }
}
