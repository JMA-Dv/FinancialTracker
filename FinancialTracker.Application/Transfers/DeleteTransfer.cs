namespace FinancialTracker.Application.Transfers;

/// <summary>Removes one of the User's Transfers, and with it its effect on both balances.</summary>
public sealed class DeleteTransfer(ITransferStore transfers)
{
    public async Task ExecuteAsync(Guid userId, Guid transferId) =>
        await transfers.RemoveAsync(await transfers.GetAsync(userId, transferId));
}
