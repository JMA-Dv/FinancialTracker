using FinancialTracker.Application.Accounts;
using FinancialTracker.Domain.Transfers;

namespace FinancialTracker.Application.Transfers;

/// <summary>Replaces every field of one of the User's Transfers.</summary>
public sealed class EditTransfer(IAccountStore accounts, ITransferStore transfers, TimeProvider clock)
{
    public async Task<Transfer> ExecuteAsync(Guid userId, Guid transferId, TransferDetails details)
    {
        var transfer = await transfers.GetAsync(userId, transferId);
        var from = await accounts.GetAsync(userId, details.FromAccountId);
        var to = await accounts.GetAsync(userId, details.ToAccountId);

        transfer.Edit(from, to, details.Date, details.Amount, details.ReceivedAmount, details.Note, clock.Today());
        await transfers.SaveChangesAsync();
        return transfer;
    }
}
