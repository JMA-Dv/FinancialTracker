using FinancialTracker.Application.Payees;
using FinancialTracker.Domain.Payees;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Infrastructure.Persistence;

public sealed class PayeeStore(FinanceDbContext dbContext) : IPayeeStore
{
    public async Task AddAsync(Payee payee)
    {
        dbContext.Payees.Add(payee);
        await dbContext.SaveChangesAsync();
    }

    public Task<Payee?> FindAsync(Guid userId, Guid payeeId) =>
        dbContext.Payees.SingleOrDefaultAsync(payee => payee.Id == payeeId && payee.UserId == userId);

    public Task<Payee?> FindByNameAsync(Guid userId, string name)
    {
        var lowered = name.ToLower();
        return dbContext.Payees.FirstOrDefaultAsync(payee => payee.UserId == userId && payee.Name.ToLower() == lowered);
    }

    public async Task<IReadOnlyList<Payee>> ListAsync(Guid userId) =>
        await dbContext.Payees.Where(payee => payee.UserId == userId).ToListAsync();

    public Task<bool> NameTakenAsync(Guid userId, string name, Guid? exceptId)
    {
        var lowered = name.ToLower();
        return dbContext.Payees.AnyAsync(payee => payee.UserId == userId && payee.Id != exceptId && payee.Name.ToLower() == lowered);
    }

    public Task SaveChangesAsync() => dbContext.SaveChangesAsync();
}
