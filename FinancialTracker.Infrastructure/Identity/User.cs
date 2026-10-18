using Microsoft.AspNetCore.Identity;

namespace FinancialTracker.Infrastructure.Identity;

/// <summary>
/// The person who signs in and owns one private set of finances.
/// </summary>
public class User : IdentityUser<Guid>
{
}
