namespace FinancialTracker.Application.Users;

/// <summary>Where Users and the Google accounts they sign in with are kept.</summary>
public interface IGoogleUserStore
{
    /// <summary>The User who signs in with the Google account, or null if it has never signed in.</summary>
    Task<GoogleUser?> FindAsync(string googleSubject);

    /// <summary>
    /// Creates a User signing in with the Google account, together with that link or not at all.
    /// Returns null if another sign-in for the same Google account created its User first.
    /// </summary>
    Task<Guid?> TryCreateAsync(string googleSubject, string email);

    /// <summary>Saves the User's new email. A concurrent save of the same change is not an error.</summary>
    Task UpdateEmailAsync(Guid userId, string email);
}

public sealed record GoogleUser(Guid Id, string? Email);
