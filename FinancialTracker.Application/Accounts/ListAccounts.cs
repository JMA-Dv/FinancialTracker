using FinancialTracker.Application.Transactions;

namespace FinancialTracker.Application.Accounts;

public sealed class ListAccounts(IAccountStore accounts, ITransactionStore transactions, TimeProvider clock)
{
    public async Task<IReadOnlyList<AccountSummary>> ExecuteAsync(Guid userId)
    {
        var today = clock.Today();
        var transactionsByAccount = (await transactions.SearchAsync(userId, TransactionFilter.All)).ToLookup(transaction => transaction.AccountId);
        return (await accounts.ListAsync(userId))
            .Select(account => AccountSummary.Of(account, transactionsByAccount[account.Id], today))
            .ToList();
    }
}
