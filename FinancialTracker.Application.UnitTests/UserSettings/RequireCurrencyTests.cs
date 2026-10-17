using FinancialTracker.Application.UserSettings;
using FinancialTracker.Domain.Money;

namespace FinancialTracker.Application.UnitTests.UserSettings;

public sealed class RequireCurrencyTests
{
    private readonly InMemoryUserSettingsStore store = new();

    [Fact]
    public async Task A_User_without_a_Currency_cant_record_money()
    {
        var userId = store.AddUser();

        await Assert.ThrowsAsync<CurrencyNotChosenException>(() => new RequireCurrency(new GetUserSettings(store)).ExecuteAsync(userId));
    }

    [Fact]
    public async Task A_User_with_a_Currency_records_money_in_it()
    {
        var userId = store.AddUser();
        await new SetCurrency(store).ExecuteAsync(userId, "MXN");

        var currency = await new RequireCurrency(new GetUserSettings(store)).ExecuteAsync(userId);

        Assert.Equal("MXN", currency.Code);
    }

    private sealed class InMemoryUserSettingsStore : IUserSettingsStore
    {
        private readonly Dictionary<Guid, Settings> settingsByUser = [];

        public Guid AddUser()
        {
            var userId = Guid.NewGuid();
            settingsByUser.Add(userId, new Settings("ana@example.test", Currency: null));
            return userId;
        }

        public Task<Settings?> GetAsync(Guid userId) => Task.FromResult(settingsByUser.GetValueOrDefault(userId));

        public Task SetCurrencyAsync(Guid userId, Currency currency)
        {
            settingsByUser[userId] = settingsByUser[userId] with { Currency = currency };
            return Task.CompletedTask;
        }
    }
}
