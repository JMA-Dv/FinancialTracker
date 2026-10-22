using FinancialTracker.Domain.Transactions;

namespace FinancialTracker.Application.Transactions;

/// <summary>Where Transactions are kept. Every read is scoped to the User who owns the Transaction.</summary>
public interface ITransactionStore
{
    Task AddAsync(Transaction transaction);

    /// <summary>The User's Transaction, or null if it doesn't exist or belongs to another User.</summary>
    Task<Transaction?> FindAsync(Guid userId, Guid transactionId);

    Task RemoveAsync(Transaction transaction);

    /// <summary>Saves changes made to Transactions found through this store.</summary>
    Task SaveChangesAsync();

    /// <summary>The User's Transactions that match the filter, newest first.</summary>
    Task<IReadOnlyList<Transaction>> SearchAsync(Guid userId, TransactionFilter filter);
}

/// <summary>Narrows a list of Transactions. A null field doesn't narrow.</summary>
/// <param name="From">The earliest date to include.</param>
/// <param name="To">The latest date to include.</param>
public sealed record TransactionFilter(Guid? AccountId, Guid? CategoryId, DateOnly? From, DateOnly? To)
{
    public static TransactionFilter All { get; } = new(null, null, null, null);

    public static TransactionFilter ForAccount(Guid accountId) => new(accountId, null, null, null);
}

public static class TransactionStoreExtensions
{
    /// <exception cref="TransactionNotFoundException">The Transaction doesn't exist or belongs to another User.</exception>
    public static async Task<Transaction> GetAsync(this ITransactionStore transactions, Guid userId, Guid transactionId) =>
        await transactions.FindAsync(userId, transactionId) ?? throw new TransactionNotFoundException(transactionId);
}
