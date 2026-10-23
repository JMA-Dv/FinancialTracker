using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Transactions;
using FinancialTracker.Domain.Transfers;

namespace FinancialTracker.Application.Accounts;

/// <summary>A Balance Adjustment as the User sees it, with the difference it made.</summary>
/// <param name="Difference">The stated balance minus what the tracker would otherwise have shown. Never income or spending.</param>
public sealed record BalanceAdjustmentSummary(Guid Id, DateOnly Date, decimal Balance, decimal Difference)
{
    public static BalanceAdjustmentSummary Of(
        Account account, BalanceAdjustment adjustment, IEnumerable<Transaction> transactions, IEnumerable<Transfer> transfers) =>
        new(adjustment.Id, adjustment.Date, adjustment.Balance, account.DifferenceOf(adjustment, transactions, transfers));
}
