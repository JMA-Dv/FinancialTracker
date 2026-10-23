using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Payees;

namespace FinancialTracker.Application.Payees;

/// <summary>A Payee a Transaction names, and whether it still has to be saved.</summary>
internal sealed record NamedPayee(Payee Payee, bool IsNew);

internal static class PayeeNames
{
    /// <summary>
    /// The User's Payee with the name, reused whatever its case, or a new one that is not saved yet so that
    /// a Transaction that turns out invalid leaves nothing behind. A blank name means no Payee.
    /// </summary>
    /// <exception cref="DomainValidationException">The name is too long.</exception>
    public static async Task<NamedPayee?> ResolveAsync(this IPayeeStore payees, Guid userId, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return await payees.FindByNameAsync(userId, name.Trim()) is { } known
            ? new NamedPayee(known, IsNew: false)
            : new NamedPayee(Payee.Create(userId, name), IsNew: true);
    }

    /// <summary>Saves the Payee if <see cref="ResolveAsync"/> had to make it.</summary>
    public static async Task SaveIfNewAsync(this IPayeeStore payees, NamedPayee? named)
    {
        if (named is { IsNew: true })
        {
            await payees.AddAsync(named.Payee);
        }
    }

    /// <exception cref="DomainConflictException">Another Payee already has the name, whatever its case.</exception>
    public static async Task EnsureFreeAsync(this IPayeeStore payees, Guid userId, string name, Guid? exceptId)
    {
        var trimmed = name.Trim();
        if (await payees.NameTakenAsync(userId, trimmed, exceptId))
        {
            throw new DomainConflictException("Payee name taken", $"There is already a Payee called {trimmed}.");
        }
    }
}
