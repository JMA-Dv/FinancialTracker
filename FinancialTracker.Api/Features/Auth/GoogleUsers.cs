using System.Security.Claims;
using FinancialTracker.Infrastructure.Identity;
using FinancialTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace FinancialTracker.Api.Features.Auth;

/// <summary>
/// Finds the User behind a Google account, creating them on their first sign-in.
/// Users are found by Google account id, never by email (ADR 0002).
/// </summary>
public sealed class GoogleUsers(UserManager<User> userManager, FinanceDbContext dbContext)
{
    public async Task<User?> FindOrCreateAsync(ExternalLoginInfo googleLogin)
    {
        var existing = await userManager.FindByLoginAsync(googleLogin.LoginProvider, googleLogin.ProviderKey);
        if (existing is not null)
        {
            return existing;
        }

        var email = googleLogin.Principal.FindFirstValue(ClaimTypes.Email);
        if (email is null)
        {
            return null;
        }

        var id = Guid.CreateVersion7();
        var user = new User { Id = id, UserName = id.ToString(), Email = email, EmailConfirmed = true };

        // The User and their Google login are created together, or not at all.
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        EnsureSucceeded(await userManager.CreateAsync(user));
        EnsureSucceeded(await userManager.AddLoginAsync(user, googleLogin));
        await transaction.CommitAsync();

        return user;
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Could not create the User for a Google sign-in: {errors}");
        }
    }
}
