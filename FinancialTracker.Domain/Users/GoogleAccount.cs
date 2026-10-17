namespace FinancialTracker.Domain.Users;

/// <summary>
/// The Google account a User signs in with. Google is the only way to sign in (ADR 0002).
/// </summary>
public sealed record GoogleAccount
{
    public GoogleAccount(string subject, string? email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        Subject = subject;
        Email = string.IsNullOrWhiteSpace(email) ? null : email;
    }

    /// <summary>Google's permanent account id (the <c>sub</c> claim). The User is found by this, never by email.</summary>
    public string Subject { get; }

    /// <summary>Display-only and not unique: two Google accounts can share one. Null when Google shared none.</summary>
    public string? Email { get; }
}
