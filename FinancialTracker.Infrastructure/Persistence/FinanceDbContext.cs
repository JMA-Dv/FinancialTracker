using FinancialTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Infrastructure.Persistence;

public class FinanceDbContext(DbContextOptions<FinanceDbContext> options)
    : IdentityUserContext<User, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>().Property(user => user.CurrencyCode).HasMaxLength(3).IsFixedLength();
    }
}
