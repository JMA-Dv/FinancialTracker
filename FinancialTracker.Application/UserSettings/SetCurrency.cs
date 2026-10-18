using FinancialTracker.Domain.Money;

namespace FinancialTracker.Application.UserSettings;

/// <summary>Sets or changes the User's Currency, the default for new Accounts (ADR 0003).</summary>
public sealed class SetCurrency(IUserSettingsStore store)
{
    public Task ExecuteAsync(Guid userId, string currencyCode) =>
        store.SetCurrencyAsync(userId, Currency.FromCode(currencyCode));
}
