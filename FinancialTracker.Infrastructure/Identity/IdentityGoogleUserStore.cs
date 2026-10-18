using FinancialTracker.Application.Users;
using FinancialTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinancialTracker.Infrastructure.Identity;

/// <summary>Keeps Users and their Google logins in ASP.NET Core Identity's tables.</summary>
public sealed class IdentityGoogleUserStore(UserManager<User> userManager, FinanceDbContext dbContext) : IGoogleUserStore
{
    /// <summary>The login provider name Google logins are stored under. Matches the Google authentication scheme.</summary>
    public const string GoogleLoginProvider = "Google";

    public async Task<GoogleUser?> FindAsync(string googleSubject)
    {
        var user = await userManager.FindByLoginAsync(GoogleLoginProvider, googleSubject);
        return user is null ? null : new GoogleUser(user.Id, user.Email);
    }

    public async Task<Guid?> TryCreateAsync(string googleSubject, string email)
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
            var linked = await userManager.AddLoginAsync(user, new UserLoginInfo(GoogleLoginProvider, googleSubject, GoogleLoginProvider));
            if (linked.Errors.Any(error => error.Code == userManager.ErrorDescriber.LoginAlreadyAssociated().Code))
            {
                return LostRace();
            }

            EnsureSucceeded(linked);
            await transaction.CommitAsync();
            return id;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return LostRace();
        }
    }

    public async Task UpdateEmailAsync(Guid userId, string email)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} does not exist.");

        // Two sign-ins refreshing at once: the other one already saved the new email, which is all this wanted.
        user.Email = email;
        var updated = await userManager.UpdateAsync(user);
        if (updated.Errors.Any(error => error.Code == userManager.ErrorDescriber.ConcurrencyFailure().Code))
        {
            return;
        }

        EnsureSucceeded(updated);
    }

    // This sign-in's changes were rolled back; forget them so the winner's User can be read.
    private Guid? LostRace()
    {
        dbContext.ChangeTracker.Clear();
        return null;
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Could not save the User for a Google sign-in: {errors}");
        }
    }
}
