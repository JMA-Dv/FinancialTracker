using FinancialTracker.Domain.Transfers;

namespace FinancialTracker.Application.Transfers;

/// <summary>Lists the User's Transfers, newest first.</summary>
public sealed class ListTransfers(ITransferStore transfers)
{
    /// <param name="accountId">Only Transfers into or out of this Account, or null for all of them.</param>
    public async Task<IReadOnlyList<Transfer>> ExecuteAsync(Guid userId, Guid? accountId) =>
        await transfers.SearchAsync(userId, accountId);
}
