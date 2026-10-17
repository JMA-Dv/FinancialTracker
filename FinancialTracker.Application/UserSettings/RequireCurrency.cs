using FinancialTracker.Domain.Money;

namespace FinancialTracker.Application.UserSettings;

/// <summary>
/// The User's Currency, for any use case that records money. Validate amounts against it with
/// <see cref="Currency.EnsureValidAmount"/>.
/// </summary>
public sealed class RequireCurrency(GetUserSettings getUserSettings)
{
    /// <exception cref="CurrencyNotChosenException">The User hasn't chosen a Currency yet.</exception>
    public async Task<Currency> ExecuteAsync(Guid userId) =>
        (await getUserSettings.ExecuteAsync(userId)).Currency ?? throw new CurrencyNotChosenException();
}
