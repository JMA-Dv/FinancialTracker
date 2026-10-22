using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Categories;
using FinancialTracker.Domain.Money;
using FinancialTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Infrastructure.Persistence;

public class FinanceDbContext(DbContextOptions<FinanceDbContext> options)
    : IdentityUserContext<User, Guid>(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>().Property(user => user.CurrencyCode).HasMaxLength(3).IsFixedLength();

        builder.Entity<Account>(account =>
        {
            account.ToTable("Accounts");
            account.HasKey(a => a.Id);
            account.Property(a => a.Id).ValueGeneratedNever();
            account.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            account.HasIndex(a => a.UserId);
            account.Property(a => a.Name).HasMaxLength(Account.MaxNameLength);
            account.Property(a => a.Kind).HasConversion<string>().HasMaxLength(20);
            account.Property(a => a.Currency)
                .HasConversion(currency => currency.Code, code => Currency.FromCode(code))
                .HasMaxLength(3)
                .IsFixedLength();
            account.HasMany(a => a.BalanceAdjustments).WithOne().HasForeignKey("AccountId").IsRequired().OnDelete(DeleteBehavior.Cascade);
            account.Navigation(a => a.BalanceAdjustments).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
            account.Ignore(a => a.OpeningBalance);
        });

        builder.Entity<Category>(category =>
        {
            category.ToTable("Categories");
            category.HasKey(c => c.Id);
            category.Property(c => c.Id).ValueGeneratedNever();
            category.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            category.HasIndex(c => c.UserId);
            category.Property(c => c.Name).HasMaxLength(Category.MaxNameLength);
            category.Property(c => c.Kind).HasConversion<string>().HasMaxLength(20);

            // Restrict: a parent is moved or archived, never deleted out from under its children.
            category.HasOne<Category>().WithMany().HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BalanceAdjustment>(adjustment =>
        {
            adjustment.ToTable("BalanceAdjustments");
            adjustment.HasKey(a => a.Id);
            adjustment.Property(a => a.Id).ValueGeneratedNever();

            // Room for 3-decimal Currencies such as KWD, and for large 0-decimal amounts such as JPY.
            adjustment.Property(a => a.Balance).HasPrecision(19, 4);
        });
    }
}
