using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.Accounts;

public sealed class BalanceAdjustmentNotFoundException(Guid balanceAdjustmentId)
    : DomainNotFoundException("Balance Adjustment not found", $"You have no Balance Adjustment {balanceAdjustmentId} on this Account.");
