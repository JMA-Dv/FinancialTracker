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

    // There are no Transactions (#7) or Transfers (#8) yet. Query them here once they exist.
    public Task<bool> HasTransactionsOrTransfersAsync(Guid accountId) => Task.FromResult(false);

    public Task SaveChangesAsync() => dbContext.SaveChangesAsync();
}
