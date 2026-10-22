using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.Transfers;

public sealed class TransferNotFoundException(Guid transferId)
    : DomainNotFoundException("Transfer not found", $"You have no Transfer {transferId}.");
