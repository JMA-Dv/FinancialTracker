using FinancialTracker.Application.Accounts;
using FinancialTracker.Domain.Transfers;

namespace FinancialTracker.Application.Transfers;

/// <summary>Records money moving between two of the User's Accounts.</summary>
public sealed class CreateTransfer(IAccountStore accounts, ITransferStore transfers, TimeProvider clock)
{
    public async Task<Transfer> ExecuteAsync(Guid userId, TransferDetails details)
    {
        var from = await accounts.GetAsync(userId, details.FromAccountId);
        var to = await accounts.GetAsync(userId, details.ToAccountId);
        var transfer = Transfer.Create(from, to, details.Date, details.Amount, details.ReceivedAmount, details.Note, clock.Today());
        await transfers.AddAsync(transfer);
        return transfer;
    }
}

/// <param name="Amount">Positive, what leaves the from-Account, in its Currency.</param>
/// <param name="ReceivedAmount">What arrives in the to-Account, in its Currency. Left out when both Accounts share a Currency.</param>
public sealed record TransferDetails(
    Guid FromAccountId, Guid ToAccountId, DateOnly Date, decimal Amount, decimal? ReceivedAmount, string? Note);
