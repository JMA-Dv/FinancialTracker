using FinancialTracker.Application.Accounts;
using FinancialTracker.Domain.Accounts;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Infrastructure.Persistence;

public sealed class AccountStore(FinanceDbContext dbContext) : IAccountStore
{
    public async Task AddAsync(Account account)
    {
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
    }

    public Task<Account?> FindAsync(Guid userId, Guid accountId) =>
        dbContext.Accounts.SingleOrDefaultAsync(account => account.Id == accountId && account.UserId == userId);

    public async Task<IReadOnlyList<Account>> ListAsync(Guid userId) =>
        // Ids are version 7 Guids, so they sort in creation order.
        await dbContext.Accounts
            .Where(account => account.UserId == userId)
            .OrderBy(account => account.Id)
            .ToListAsync();

    // Transfers (#8) don't exist yet. Query them here once they do.
    public Task<bool> HasTransactionsOrTransfersAsync(Guid accountId) =>
        dbContext.Transactions.AnyAsync(transaction => transaction.AccountId == accountId);

    public Task SaveChangesAsync() => dbContext.SaveChangesAsync();
}
