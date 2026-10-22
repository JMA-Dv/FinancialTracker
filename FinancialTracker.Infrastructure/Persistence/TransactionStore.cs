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
            .Where(transaction => filter.CategoryId == null || transaction.CategoryId == filter.CategoryId)
            .Where(transaction => filter.From == null || transaction.Date >= filter.From)
            .Where(transaction => filter.To == null || transaction.Date <= filter.To)
            .OrderByDescending(transaction => transaction.Date)
            .ThenByDescending(transaction => transaction.Id)
            .ToListAsync();
}
