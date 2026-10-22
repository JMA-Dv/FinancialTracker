using FinancialTracker.Domain.Accounts;

namespace FinancialTracker.Application.Accounts;

/// <summary>Where Accounts are kept. Every read is scoped to the User who owns the Account.</summary>
public interface IAccountStore
{
    Task AddAsync(Account account);

    /// <summary>The User's Account, or null if it doesn't exist or belongs to another User.</summary>
    Task<Account?> FindAsync(Guid userId, Guid accountId);

    /// <summary>The User's Accounts, in the order they were created.</summary>
    Task<IReadOnlyList<Account>> ListAsync(Guid userId);

    /// <summary>Whether any Transaction or Transfer uses the Account.</summary>
    Task<bool> HasTransactionsOrTransfersAsync(Guid accountId);

    /// <summary>Saves changes made to Accounts found through this store.</summary>
    Task SaveChangesAsync();
}

public static class AccountStoreExtensions
{
    /// <exception cref="AccountNotFoundException">The Account doesn't exist or belongs to another User.</exception>
    public static async Task<Account> GetAsync(this IAccountStore accounts, Guid userId, Guid accountId) =>
        await accounts.FindAsync(userId, accountId) ?? throw new AccountNotFoundException(accountId);
}
