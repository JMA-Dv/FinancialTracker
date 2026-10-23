using FinancialTracker.Application.Transactions;
using FinancialTracker.Application.Transfers;

namespace FinancialTracker.Application.Accounts;

/// <summary>Lists everything that happened on an Account: its Transactions, Transfers and Balance Adjustments, newest first.
/// Money dated on or before a Balance Adjustment is listed too, though the balance it states already includes it.</summary>
public sealed class GetAccountHistory(IAccountStore accounts, ITransactionStore transactions, ITransferStore transfers)
{
    public async Task<IReadOnlyList<AccountHistoryEntry>> ExecuteAsync(Guid userId, Guid accountId)
    {
        var account = await accounts.GetAsync(userId, accountId);
        var accountTransactions = await transactions.SearchAsync(userId, TransactionFilter.ForAccount(account.Id));
        var accountTransfers = await transfers.SearchAsync(userId, account.Id);

        return accountTransactions
            .Select(t => new AccountHistoryEntry(AccountHistoryKind.Transaction, t.Id, t.Date, t.SignedAmount, StatedBalance: null))
            .Concat(accountTransfers.Select(t =>
                new AccountHistoryEntry(AccountHistoryKind.Transfer, t.Id, t.Date, t.EffectOn(account.Id), StatedBalance: null)))
            .Concat(account.BalanceAdjustments.Select(a =>
                new AccountHistoryEntry(
                    AccountHistoryKind.BalanceAdjustment, a.Id, a.Date, account.DifferenceOf(a, accountTransactions, accountTransfers), a.Balance)))
            // A Balance Adjustment states the balance at the end of its date, so it comes after that day's money (and so first when newest is first).
            .OrderByDescending(entry => entry.Date)
            .ThenByDescending(entry => entry.Kind is AccountHistoryKind.BalanceAdjustment)
            .ThenByDescending(entry => entry.Id)
            .ToList();
    }
}

public enum AccountHistoryKind
{
    Transaction,
    Transfer,
    BalanceAdjustment,
}

/// <param name="Id">The id of the Transaction, Transfer or Balance Adjustment, whichever the <paramref name="Kind"/> says.</param>
/// <param name="Amount">How it changed the Account's balance, signed. For a Balance Adjustment, the difference it made.</param>
/// <param name="StatedBalance">The balance a Balance Adjustment states. Null for the others.</param>
public sealed record AccountHistoryEntry(AccountHistoryKind Kind, Guid Id, DateOnly Date, decimal Amount, decimal? StatedBalance);
