using FinancialTracker.Application.Accounts;
using FinancialTracker.Application.Transactions;
using FinancialTracker.Application.Transfers;
using FinancialTracker.Domain.Common;

namespace FinancialTracker.Application.Reports;

/// <summary>Adds up what the User's Accounts held at the end of a date, per Currency.</summary>
public sealed class GetNetWorth(IAccountStore accounts, ITransactionStore transactions, ITransferStore transfers, TimeProvider clock)
{
    /// <param name="date">The day to report on, or null for today.</param>
    /// <exception cref="DomainValidationException">The date is after today.</exception>
    public async Task<NetWorth> ExecuteAsync(Guid userId, DateOnly? date)
    {
        var today = clock.Today();
        var on = date ?? today;
        if (on > today)
        {
            throw new DomainValidationException("Date in the future", $"{on:yyyy-MM-dd} is after today ({today:yyyy-MM-dd}).");
        }

        // Closed Accounts are included: they held money on the dates before they were closed.
        var allAccounts = await accounts.ListAsync(userId);
        var allTransactions = await transactions.SearchAsync(userId, TransactionFilter.All with { To = on });
        var allTransfers = await transfers.SearchAsync(userId, accountId: null);

        // A Liability Account's balance is already negative when money is owed, so plain addition nets it off.
        var totals = allAccounts
            .GroupBy(account => account.Currency)
            .Select(group => new CurrencyTotal(group.Key, group.Sum(account => account.BalanceOn(on, allTransactions, allTransfers))))
            .OrderBy(total => total.Currency.Code, StringComparer.Ordinal)
            .ToList();
        return new NetWorth(on, totals);
    }
}

/// <param name="Date">The day the balances are at the end of.</param>
public sealed record NetWorth(DateOnly Date, IReadOnlyList<CurrencyTotal> Totals);
