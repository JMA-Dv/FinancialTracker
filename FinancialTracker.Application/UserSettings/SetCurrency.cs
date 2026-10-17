using FinancialTracker.Domain.Money;

namespace FinancialTracker.Application.UserSettings;

/// <summary>Sets or changes the one Currency all of a User's finances are kept in (ADR 0001).</summary>
public sealed class SetCurrency(IUserSettingsStore store)
{
    public Task ExecuteAsync(Guid userId, string currencyCode) =>
        store.SetCurrencyAsync(userId, Currency.FromCode(currencyCode));
}
