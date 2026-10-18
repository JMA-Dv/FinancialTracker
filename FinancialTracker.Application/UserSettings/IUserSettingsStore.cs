using FinancialTracker.Domain.Money;

namespace FinancialTracker.Application.UserSettings;

/// <summary>Where each User's settings are kept.</summary>
public interface IUserSettingsStore
{
    /// <summary>The User's settings, or null if the User doesn't exist.</summary>
    Task<Settings?> GetAsync(Guid userId);

    Task SetCurrencyAsync(Guid userId, Currency currency);
}

/// <summary>A User's settings. Currency is null until the User chooses one.</summary>
public sealed record Settings(string Email, Currency? Currency);
