using FinancialTracker.Application.Transactions;
using FinancialTracker.Application.Transfers;
using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Transactions;
using FinancialTracker.Domain.Transfers;

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

    /// <summary>A Balance Adjustment of the Account, with the difference it made.</summary>
    public async Task<BalanceAdjustmentSummary> SummarizeAsync(Guid userId, Account account, BalanceAdjustment adjustment)
    {
        var (accountTransactions, accountTransfers) = await MoneyOfAsync(userId, account);
        return BalanceAdjustmentSummary.Of(account, adjustment, accountTransactions, accountTransfers);
    }

    /// <summary>The Account's Balance Adjustments, newest first.</summary>
    public async Task<IReadOnlyList<BalanceAdjustmentSummary>> SummarizeAdjustmentsAsync(Guid userId, Account account)
    {
        var (accountTransactions, accountTransfers) = await MoneyOfAsync(userId, account);
        return account.BalanceAdjustments
            .OrderByDescending(adjustment => adjustment.Date)
            .Select(adjustment => BalanceAdjustmentSummary.Of(account, adjustment, accountTransactions, accountTransfers))
            .ToList();
    }

    /// <summary>What the Account held at the end of the date.</summary>
    public async Task<decimal> BalanceOnAsync(Guid userId, Account account, DateOnly date)
    {
        var (accountTransactions, accountTransfers) = await MoneyOfAsync(userId, account);
        return account.BalanceOn(date, accountTransactions, accountTransfers);
    }

    private async Task<(IReadOnlyList<Transaction> Transactions, IReadOnlyList<Transfer> Transfers)> MoneyOfAsync(
        Guid userId, Account account) =>
        (await transactions.SearchAsync(userId, TransactionFilter.ForAccount(account.Id)),
            await transfers.SearchAsync(userId, account.Id));
}
