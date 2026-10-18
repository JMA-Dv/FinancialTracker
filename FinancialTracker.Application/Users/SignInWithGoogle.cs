using FinancialTracker.Domain.Users;

namespace FinancialTracker.Application.Users;

/// <summary>
/// Finds the User behind a Google account, creating them on their first sign-in.
/// Users are found by Google account id, never by email (ADR 0002).
/// </summary>
public sealed class SignInWithGoogle(IGoogleUserStore store)
{
    /// <summary>The id of the User to sign in, or null when it's a first sign-in and Google shared no email.</summary>
    public async Task<Guid?> ExecuteAsync(GoogleAccount google)
    {
        ArgumentNullException.ThrowIfNull(google);

        var existing = await store.FindAsync(google.Subject);
        if (existing is not null)
        {
            // The email is display-only, so it follows whatever the Google account says now.
            if (google.Email is not null && !string.Equals(existing.Email, google.Email, StringComparison.Ordinal))
            {
                await store.UpdateEmailAsync(existing.Id, google.Email);
            }

            return existing.Id;
        }

        if (google.Email is null)
        {
            return null;
        }

        var created = await store.TryCreateAsync(google.Subject, google.Email);
        if (created is not null)
        {
            return created;
        }

        // Two first sign-ins for the same Google account raced (a double-click, two tabs), and the
        // other one created the User. Sign in as the User it created.
        var winner = await store.FindAsync(google.Subject)
            ?? throw new InvalidOperationException("Could not create the User for a Google sign-in.");
        return winner.Id;
    }
}
