using FinancialTracker.Domain.Transfers;

namespace FinancialTracker.Application.Transfers;

public sealed class GetTransfer(ITransferStore transfers)
{
    public async Task<Transfer> ExecuteAsync(Guid userId, Guid transferId) => await transfers.GetAsync(userId, transferId);
}
