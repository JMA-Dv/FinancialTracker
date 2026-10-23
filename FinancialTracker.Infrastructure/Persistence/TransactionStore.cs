using FinancialTracker.Application.Transactions;
using FinancialTracker.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Infrastructure.Persistence;

public sealed class TransactionStore(FinanceDbContext dbContext) : ITransactionStore
{
    public async Task AddAsync(Transaction transaction)
    {
        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();
    }

    public Task<Transaction?> FindAsync(Guid userId, Guid transactionId) =>
        dbContext.Transactions.SingleOrDefaultAsync(transaction => transaction.Id == transactionId && transaction.UserId == userId);

    public async Task RemoveAsync(Transaction transaction)
    {
        dbContext.Transactions.Remove(transaction);
        await dbContext.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => dbContext.SaveChangesAsync();

    public async Task<IReadOnlyList<Transaction>> SearchAsync(Guid userId, TransactionFilter filter) =>
        // Ids are version 7 Guids, so they break ties between Transactions on the same date in creation order.
        await dbContext.Transactions
            .Where(transaction => transaction.UserId == userId)
            .Where(transaction => filter.AccountId == null || transaction.AccountId == filter.AccountId)
            // Categories have two levels at most, so a parent's children are everything under it.
            .Where(transaction => filter.CategoryId == null
                || dbContext.Categories.Any(category => category.Id == transaction.CategoryId
                    && (category.Id == filter.CategoryId || category.ParentId == filter.CategoryId)))
            .Where(transaction => filter.PayeeId == null || transaction.PayeeId == filter.PayeeId)
            .Where(transaction => filter.From == null || transaction.Date >= filter.From)
            .Where(transaction => filter.To == null || transaction.Date <= filter.To)
            .OrderByDescending(transaction => transaction.Date)
            .ThenByDescending(transaction => transaction.Id)
            .ToListAsync();

    public async Task<IReadOnlyDictionary<Guid, Guid>> LatestCategoriesByPayeeAsync(Guid userId, Guid? payeeId)
    {
        var latest = await dbContext.Transactions
            .Where(transaction => transaction.UserId == userId && transaction.PayeeId != null)
            .Where(transaction => payeeId == null || transaction.PayeeId == payeeId)
            .GroupBy(transaction => transaction.PayeeId!.Value)
            .Select(group => new
            {
                PayeeId = group.Key,
                // Same order as the list: the newest date wins, then the last one recorded.
                CategoryId = group.OrderByDescending(transaction => transaction.Date).ThenByDescending(transaction => transaction.Id)
                    .Select(transaction => transaction.CategoryId).First(),
            })
            .ToListAsync();
        return latest.ToDictionary(entry => entry.PayeeId, entry => entry.CategoryId);
    }
}
