using FinancialTracker.Application.UserSettings;
using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Money;

namespace FinancialTracker.Application.Accounts;

/// <summary>Adds a place where the User's money sits.</summary>
public sealed class OpenAccount(IAccountStore accounts, GetUserSettings getUserSettings, TimeProvider clock)
{
    public async Task<AccountSummary> ExecuteAsync(Guid userId, OpenAccountRequest request)
    {
        // The User's Currency is only the default for new Accounts (ADR 0003).
        var currency = request.CurrencyCode is null
            ? (await getUserSettings.ExecuteAsync(userId)).Currency
                ?? throw new DomainValidationException(
                    "Currency required",
                    "Give the Account a Currency, or set your Currency in user settings to use it by default.")
            : Currency.FromCode(request.CurrencyCode);

        var today = clock.Today();
        var account = Account.Open(userId, request.Name, request.Kind, currency, request.OpeningBalance, today);
        await accounts.AddAsync(account);

        return AccountSummary.Of(account, today);
    }
}

/// <param name="CurrencyCode">The Account's ISO 4217 Currency, or null for the User's Currency.</param>
/// <param name="OpeningBalance">What the Account held when tracking starts, or null to start at zero.</param>
public sealed record OpenAccountRequest(string Name, AccountKind Kind, string? CurrencyCode, OpeningBalance? OpeningBalance);
