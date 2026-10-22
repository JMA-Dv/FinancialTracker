using System.Security.Claims;

namespace FinancialTracker.Api.Features.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in User's id. Every endpoint that isn't anonymous has one.</summary>
    public static Guid Id(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("A signed-in User always carries their id.");
}
