using FinancialTracker.Application.Payees;
using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Payees;
using FinancialTracker.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinancialTracker.Infrastructure.Persistence;

public sealed class PayeeStore(FinanceDbContext dbContext) : IPayeeStore
{
    public async Task AddAsync(Payee payee)
    {
        dbContext.Payees.Add(payee);
        await SaveChangesAsync();
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

    /// <exception cref="DomainConflictException">Another request gave a Payee the same name, whatever its case, at the same time.</exception>
    public async Task SaveChangesAsync()
    {
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } violation
            && violation.ConstraintName == AddPayeeNameUniqueness.PayeeNameIndex)
        {
            // The name check ran before the other request saved. The caller can retry and will find the Payee.
            throw new DomainConflictException("Payee name taken", "Another request just used that Payee name. Try again.");
        }
    }
}
