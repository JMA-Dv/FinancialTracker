using FinancialTracker.Application.Transfers;
using FinancialTracker.Domain.Transfers;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Infrastructure.Persistence;

public sealed class TransferStore(FinanceDbContext dbContext) : ITransferStore
{
    public async Task AddAsync(Transfer transfer)
    {
        dbContext.Transfers.Add(transfer);
        await dbContext.SaveChangesAsync();
    }

    public Task<Transfer?> FindAsync(Guid userId, Guid transferId) =>
        dbContext.Transfers.SingleOrDefaultAsync(transfer => transfer.Id == transferId && transfer.UserId == userId);

    public async Task RemoveAsync(Transfer transfer)
    {
        dbContext.Transfers.Remove(transfer);
        await dbContext.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => dbContext.SaveChangesAsync();

    public async Task<IReadOnlyList<Transfer>> SearchAsync(Guid userId, Guid? accountId) =>
        // Ids are version 7 Guids, so they break ties between Transfers on the same date in creation order.
        await dbContext.Transfers
            .Where(transfer => transfer.UserId == userId)
            .Where(transfer => accountId == null || transfer.FromAccountId == accountId || transfer.ToAccountId == accountId)
            .OrderByDescending(transfer => transfer.Date)
            .ThenByDescending(transfer => transfer.Id)
            .ToListAsync();
}
