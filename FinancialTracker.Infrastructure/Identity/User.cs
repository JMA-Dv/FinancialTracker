using Microsoft.AspNetCore.Identity;

namespace FinancialTracker.Infrastructure.Identity;

/// <summary>
/// The person who signs in and owns one private set of finances.
/// </summary>
public class User : IdentityUser<Guid>
{
    /// <summary>The ISO 4217 code of the User's Currency (ADR 0001). Null until the User chooses one.</summary>
    public string? CurrencyCode { get; set; }
}
