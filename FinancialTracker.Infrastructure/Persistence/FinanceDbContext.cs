using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Categories;
using FinancialTracker.Domain.Money;
using FinancialTracker.Domain.Transactions;
using FinancialTracker.Domain.Transfers;
using FinancialTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Infrastructure.Persistence;

public class FinanceDbContext(DbContextOptions<FinanceDbContext> options)
    : IdentityUserContext<User, Guid>(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Transfer> Transfers => Set<Transfer>();

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

        builder.Entity<Transaction>(transaction =>
        {
            transaction.ToTable("Transactions");
            transaction.HasKey(t => t.Id);
            transaction.Property(t => t.Id).ValueGeneratedNever();
            transaction.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            // Restrict: money that moved through an Account or Category is never deleted along with it.
            transaction.HasOne<Account>().WithMany().HasForeignKey(t => t.AccountId).OnDelete(DeleteBehavior.Restrict);
            transaction.HasOne<Category>().WithMany().HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.Restrict);
            transaction.HasIndex(t => t.UserId);
            transaction.HasIndex(t => t.AccountId);
            transaction.Property(t => t.Direction).HasConversion<string>().HasMaxLength(3);
            transaction.Property(t => t.Amount).HasPrecision(19, 4);
            transaction.Property(t => t.Note).HasMaxLength(Transaction.MaxNoteLength);
            transaction.Ignore(t => t.SignedAmount);
        });

        builder.Entity<Transfer>(transfer =>
        {
            transfer.ToTable("Transfers");
            transfer.HasKey(t => t.Id);
            transfer.Property(t => t.Id).ValueGeneratedNever();
            transfer.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            // Restrict: money that moved through an Account is never deleted along with it.
            transfer.HasOne<Account>().WithMany().HasForeignKey(t => t.FromAccountId).OnDelete(DeleteBehavior.Restrict);
            transfer.HasOne<Account>().WithMany().HasForeignKey(t => t.ToAccountId).OnDelete(DeleteBehavior.Restrict);
            transfer.HasIndex(t => t.UserId);
            transfer.HasIndex(t => t.FromAccountId);
            transfer.HasIndex(t => t.ToAccountId);
            transfer.Property(t => t.AmountSent).HasPrecision(19, 4);
            transfer.Property(t => t.AmountReceived).HasPrecision(19, 4);
            transfer.Property(t => t.Note).HasMaxLength(Transfer.MaxNoteLength);
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
