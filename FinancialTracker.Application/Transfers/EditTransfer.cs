using FinancialTracker.Application.Accounts;
using FinancialTracker.Domain.Transfers;

namespace FinancialTracker.Application.Transfers;

/// <summary>Replaces every field of one of the User's Transfers.</summary>
public sealed class EditTransfer(IAccountStore accounts, ITransferStore transfers, TimeProvider clock)
{
    public async Task<Transfer> ExecuteAsync(Guid userId, Guid transferId, TransferDetails details)
    {
        var transfer = await transfers.GetAsync(userId, transferId);
        var currentFrom = await accounts.GetAsync(userId, transfer.FromAccountId);
        var currentTo = await accounts.GetAsync(userId, transfer.ToAccountId);
        var from = details.FromAccountId == currentFrom.Id ? currentFrom : await accounts.GetAsync(userId, details.FromAccountId);
        var to = details.ToAccountId == currentTo.Id ? currentTo : await accounts.GetAsync(userId, details.ToAccountId);

        transfer.Edit(currentFrom, currentTo, from, to, details.Date, details.Amount, details.ReceivedAmount, details.Note, clock.Today());
        await transfers.SaveChangesAsync();
        return transfer;
    }
}
