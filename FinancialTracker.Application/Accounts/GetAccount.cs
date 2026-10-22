using FinancialTracker.Application.Transactions;

namespace FinancialTracker.Application.Accounts;

public sealed class GetAccount(IAccountStore accounts, ITransactionStore transactions, TimeProvider clock)
{
    public async Task<AccountSummary> ExecuteAsync(Guid userId, Guid accountId)
    {
        var account = await accounts.GetAsync(userId, accountId);
        return AccountSummary.Of(account, await transactions.SearchAsync(userId, TransactionFilter.ForAccount(account.Id)), clock.Today());
    }
}
