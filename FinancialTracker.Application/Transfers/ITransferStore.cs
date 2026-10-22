using FinancialTracker.Domain.Transfers;

namespace FinancialTracker.Application.Transfers;

/// <summary>Where Transfers are kept. Every read is scoped to the User who owns the Transfer.</summary>
public interface ITransferStore
{
    Task AddAsync(Transfer transfer);

    /// <summary>The User's Transfer, or null if it doesn't exist or belongs to another User.</summary>
    Task<Transfer?> FindAsync(Guid userId, Guid transferId);

    Task RemoveAsync(Transfer transfer);

    /// <summary>Saves changes made to Transfers found through this store.</summary>
    Task SaveChangesAsync();

    /// <summary>The User's Transfers, newest first, optionally only those into or out of one Account.</summary>
    Task<IReadOnlyList<Transfer>> SearchAsync(Guid userId, Guid? accountId);
}

public static class TransferStoreExtensions
{
    /// <exception cref="TransferNotFoundException">The Transfer doesn't exist or belongs to another User.</exception>
    public static async Task<Transfer> GetAsync(this ITransferStore transfers, Guid userId, Guid transferId) =>
        await transfers.FindAsync(userId, transferId) ?? throw new TransferNotFoundException(transferId);
}
