using FinancialTracker.Application.UserSettings;
using FinancialTracker.Domain.Money;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Infrastructure.Persistence;

/// <summary>Keeps each User's settings on their Identity User row.</summary>
public sealed class UserSettingsStore(FinanceDbContext dbContext) : IUserSettingsStore
{
    public async Task<Settings?> GetAsync(Guid userId)
    {
        var row = await dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => new { user.Email, user.CurrencyCode })
            .SingleOrDefaultAsync();
        if (row is null)
        {
            return null;
        }

        var email = row.Email
            ?? throw new InvalidOperationException($"User {userId} has no email, but every User is created with one.");

        if (row.CurrencyCode is null)
        {
            return new Settings(email, Currency: null);
        }

        return Currency.TryFromCode(row.CurrencyCode, out var currency)
            ? new Settings(email, currency)
            : throw new InvalidOperationException(
                $"User {userId} has Currency '{row.CurrencyCode}', which is no longer a known ISO 4217 code.");
    }

    public async Task SetCurrencyAsync(Guid userId, Currency currency)
    {
        var updated = await dbContext.Users
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.CurrencyCode, currency.Code));
        if (updated is 0)
        {
            throw new InvalidOperationException($"User {userId} does not exist.");
        }
    }
}
