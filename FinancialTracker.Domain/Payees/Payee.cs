using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.Payees;

/// <summary>The person or business a User paid or was paid by, kept as a reusable name across Transactions.</summary>
public sealed class Payee
{
    public const int MaxNameLength = 100;

    private Payee(Guid id, Guid userId, string name)
    {
        Id = id;
        UserId = userId;
        Name = name;
    }

    public Guid Id { get; private set; }

    /// <summary>The User who owns this Payee. Nobody else can see or use it.</summary>
    public Guid UserId { get; private set; }

    public string Name { get; private set; }

    /// <exception cref="DomainValidationException">The name is blank or too long.</exception>
    public static Payee Create(Guid userId, string name) => new(Guid.CreateVersion7(), userId, ValidName(name));

    /// <exception cref="DomainValidationException">The name is blank or too long.</exception>
    public void Rename(string name) => Name = ValidName(name);

    private static string ValidName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length is 0 or > MaxNameLength)
        {
            throw new DomainValidationException("Invalid Payee name", $"A Payee's name must have 1 to {MaxNameLength} characters.");
        }

        return trimmed;
    }
}
