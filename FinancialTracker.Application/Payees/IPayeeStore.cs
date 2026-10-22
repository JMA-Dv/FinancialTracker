using FinancialTracker.Domain.Payees;

namespace FinancialTracker.Application.Payees;

/// <summary>Where Payees are kept. Every read is scoped to the User who owns the Payee.</summary>
public interface IPayeeStore
{
    Task AddAsync(Payee payee);

    /// <summary>The User's Payee, or null if it doesn't exist or belongs to another User.</summary>
    Task<Payee?> FindAsync(Guid userId, Guid payeeId);

    /// <summary>The User's Payee with the name, ignoring case, or null if they have none.</summary>
    Task<Payee?> FindByNameAsync(Guid userId, string name);

    /// <summary>All of the User's Payees.</summary>
    Task<IReadOnlyList<Payee>> ListAsync(Guid userId);

    /// <summary>
    /// Whether the User has a Payee with the name, ignoring case.
    /// <paramref name="exceptId"/> is left out, so a Payee doesn't clash with itself.
    /// </summary>
    Task<bool> NameTakenAsync(Guid userId, string name, Guid? exceptId);

    /// <summary>Saves changes made to Payees found through this store.</summary>
    Task SaveChangesAsync();
}

public static class PayeeStoreExtensions
{
    /// <exception cref="PayeeNotFoundException">The Payee doesn't exist or belongs to another User.</exception>
    public static async Task<Payee> GetAsync(this IPayeeStore payees, Guid userId, Guid payeeId) =>
        await payees.FindAsync(userId, payeeId) ?? throw new PayeeNotFoundException(payeeId);
}
