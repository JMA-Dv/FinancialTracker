using FinancialTracker.Application.Transactions;
using FinancialTracker.Application.Transfers;
using FinancialTracker.Domain.Accounts;

namespace FinancialTracker.Application.Accounts;

/// <summary>Works out Accounts' balances from the money that moved through them.</summary>
public sealed class AccountSummarizer(ITransactionStore transactions, ITransferStore transfers, TimeProvider clock)
{
    public async Task<AccountSummary> SummarizeAsync(Guid userId, Account account) =>
        AccountSummary.Of(
            account,
            await transactions.SearchAsync(userId, TransactionFilter.ForAccount(account.Id)),
            await transfers.SearchAsync(userId, account.Id),
            clock.Today());

    public async Task<IReadOnlyList<AccountSummary>> SummarizeAsync(Guid userId, IReadOnlyList<Account> accounts)
    {
        var allTransactions = await transactions.SearchAsync(userId, TransactionFilter.All);
        var allTransfers = await transfers.SearchAsync(userId, accountId: null);
        var today = clock.Today();
        return accounts.Select(account => AccountSummary.Of(account, allTransactions, allTransfers, today)).ToList();
    }
}
