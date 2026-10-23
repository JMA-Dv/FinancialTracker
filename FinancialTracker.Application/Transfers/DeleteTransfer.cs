using FinancialTracker.Application.Accounts;

namespace FinancialTracker.Application.Transfers;

/// <summary>Removes one of the User's Transfers, and with it its effect on both balances.</summary>
public sealed class DeleteTransfer(IAccountStore accounts, ITransferStore transfers)
{
    public async Task ExecuteAsync(Guid userId, Guid transferId)
    {
        var transfer = await transfers.GetAsync(userId, transferId);

        // A Closed Account holds a zero balance, which removing money would change.
        (await accounts.GetAsync(userId, transfer.FromAccountId)).EnsureOpen();
        (await accounts.GetAsync(userId, transfer.ToAccountId)).EnsureOpen();
        await transfers.RemoveAsync(transfer);
    }
}
