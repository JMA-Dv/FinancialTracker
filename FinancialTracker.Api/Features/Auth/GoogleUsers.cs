using System.Security.Claims;
using FinancialTracker.Infrastructure.Identity;
using FinancialTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinancialTracker.Api.Features.Auth;

/// <summary>
/// Finds the User behind a Google account, creating them on their first sign-in.
/// Users are found by Google account id, never by email (ADR 0002).
/// </summary>
public sealed class GoogleUsers(UserManager<User> userManager, FinanceDbContext dbContext)
{
    public async Task<User?> FindOrCreateAsync(ExternalLoginInfo googleLogin)
    {
        var email = googleLogin.Principal.FindFirstValue(ClaimTypes.Email);

        var existing = await userManager.FindByLoginAsync(googleLogin.LoginProvider, googleLogin.ProviderKey);
        if (existing is not null)
        {
            await RefreshEmailAsync(existing, email);
            return existing;
        }

        if (email is null)
        {
            return null;
        }

        var created = await TryCreateAsync(googleLogin, email);
        if (created is not null)
        {
            return created;
        }

        // Two first sign-ins for the same Google account raced (a double-click, two tabs), and the
        // other one created the User. This one rolled back, so sign in as the User it created.
        dbContext.ChangeTracker.Clear();
        return await userManager.FindByLoginAsync(googleLogin.LoginProvider, googleLogin.ProviderKey)
            ?? throw new InvalidOperationException("Could not create the User for a Google sign-in.");
    }

    /// <summary>Creates the User with their Google login, or returns null if another sign-in got there first.</summary>
    private async Task<User?> TryCreateAsync(ExternalLoginInfo googleLogin, string email)
    {
        var id = Guid.CreateVersion7();
        var user = new User { Id = id, UserName = id.ToString(), Email = email, EmailConfirmed = true };

        // The User and their Google login are created together, or not at all.
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            EnsureSucceeded(await userManager.CreateAsync(user));

            // Identity reports a login the other sign-in already committed as a failed result,
            // and one it's still committing as a unique-key violation.
            var linked = await userManager.AddLoginAsync(user, googleLogin);
            if (linked.Errors.Any(error => error.Code == userManager.ErrorDescriber.LoginAlreadyAssociated().Code))
            {
                return null;
            }

            EnsureSucceeded(linked);
            await transaction.CommitAsync();
            return user;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return null;
        }
    }

    // The email is display-only, so it follows whatever the Google account says now.
    private async Task RefreshEmailAsync(User user, string? email)
    {
        if (email is null || string.Equals(user.Email, email, StringComparison.Ordinal))
        {
            return;
        }

        // Two sign-ins refreshing at once: the other one already saved the new email, which is all this wanted.
        user.Email = email;
        var updated = await userManager.UpdateAsync(user);
        if (updated.Errors.Any(error => error.Code == userManager.ErrorDescriber.ConcurrencyFailure().Code))
        {
            return;
        }

        EnsureSucceeded(updated);
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
