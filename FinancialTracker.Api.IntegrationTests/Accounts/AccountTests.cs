using System.Net;
using System.Net.Http.Json;
using FinancialTracker.Api.IntegrationTests.Infrastructure;

namespace FinancialTracker.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class AccountTests(ApiFactory api)
{
    private const string AccountsEndpoint = "/api/accounts";

    [Fact]
    public async Task An_Account_created_without_an_Opening_Balance_is_listed_at_zero_in_the_Users_Currency()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("MXN");

        var created = await user.Client.PostAsJsonAsync(AccountsEndpoint, new { name = "Wallet", kind = "Cash" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var accounts = await user.Client.GetFromJsonAsync<List<AccountView>>(AccountsEndpoint);
        var wallet = Assert.Single(accounts!);
        Assert.Equal(new AccountView(wallet.Id, "Wallet", "Cash", "MXN", 0m, IsLiability: false, OpeningBalance: null), wallet);
    }

    [Fact]
    public async Task A_Credit_Card_opened_owing_500_is_a_Liability_Account_with_a_balance_of_minus_500()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("MXN");
        var trackingStarts = Today.AddDays(-30);

        var created = await user.Client.PostAsJsonAsync(AccountsEndpoint, new
        {
            name = "Visa",
            kind = "CreditCard",
            currency = "USD",
            openingBalance = new { amount = -500m, date = trackingStarts },
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var visa = Assert.Single((await user.Client.GetFromJsonAsync<List<AccountView>>(AccountsEndpoint))!);
        Assert.Equal(
            new AccountView(visa.Id, "Visa", "CreditCard", "USD", -500m, IsLiability: true, new OpeningBalanceView(-500m, trackingStarts)),
            visa);
    }

    public static TheoryData<string, object> InvalidAccounts => new()
    {
        { "an Opening Balance dated in the future", new { name = "Checking", kind = "Checking", openingBalance = new { amount = 100m, date = Today.AddDays(1) } } },
        { "an Opening Balance with more decimal places than USD allows", new { name = "Checking", kind = "Checking", openingBalance = new { amount = 10.005m, date = Today } } },
        { "an Opening Balance with decimals in JPY", new { name = "Checking", kind = "Checking", currency = "JPY", openingBalance = new { amount = 10.5m, date = Today } } },
        { "an Opening Balance too large for the database", new { name = "Checking", kind = "Checking", openingBalance = new { amount = 1e20m, date = Today } } },
        { "an unknown Currency", new { name = "Checking", kind = "Checking", currency = "ZZZ" } },
        { "a blank name", new { name = "  ", kind = "Checking" } },
        { "a name over 100 characters", new { name = new string('a', 101), kind = "Checking" } },
        { "no kind", new { name = "Checking" } },
        { "a kind that doesn't exist", new { name = "Checking", kind = 99 } },
        { "an Opening Balance without a date", new { name = "Checking", kind = "Checking", openingBalance = new { amount = 100m } } },
        { "an Opening Balance without an amount", new { name = "Checking", kind = "Checking", openingBalance = new { date = Today } } },
    };

    [Theory]
    [MemberData(nameof(InvalidAccounts))]
    public async Task An_Account_that_breaks_a_rule_is_rejected_and_not_created(string rule, object account)
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");

        var created = await user.Client.PostAsJsonAsync(AccountsEndpoint, account);

        Assert.True(created.StatusCode is HttpStatusCode.BadRequest, $"{rule}: got {(int)created.StatusCode}");
        Assert.Empty((await user.Client.GetFromJsonAsync<List<AccountView>>(AccountsEndpoint))!);
    }

    [Fact]
    public async Task An_Account_needs_a_Currency_when_the_User_has_none()
    {
        var user = await api.CreateSignedInUserAsync();

        var withoutCurrency = await user.Client.PostAsJsonAsync(AccountsEndpoint, new { name = "Wallet", kind = "Cash" });
        var withCurrency = await user.Client.PostAsJsonAsync(AccountsEndpoint, new { name = "Wallet", kind = "Cash", currency = "EUR" });

        Assert.Equal(HttpStatusCode.BadRequest, withoutCurrency.StatusCode);
        Assert.Equal(HttpStatusCode.Created, withCurrency.StatusCode);
        Assert.Equal("EUR", (await withCurrency.Content.ReadFromJsonAsync<AccountView>())?.Currency);
    }

    [Fact]
    public async Task A_User_can_get_one_Account_and_rename_it()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var account = await CreateAsync(user, new { name = "Chequing", kind = "Checking" });

        var renamed = await user.Client.PutAsJsonAsync($"{AccountsEndpoint}/{account.Id}/name", new { name = " Checking " });

        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        var fetched = await user.Client.GetFromJsonAsync<AccountView>($"{AccountsEndpoint}/{account.Id}");
        Assert.Equal(account with { Name = "Checking" }, fetched);
    }

    [Fact]
    public async Task A_User_can_change_the_Currency_of_an_Account_that_has_no_money_moving_through_it()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var account = await CreateAsync(user, new { name = "Savings", kind = "Savings", openingBalance = new { amount = 10.50m, date = Today } });
        var url = $"{AccountsEndpoint}/{account.Id}";

        var toJpy = await user.Client.PutAsJsonAsync(url + "/currency", new { currency = "JPY" });
        var toMxn = await user.Client.PutAsJsonAsync(url + "/currency", new { currency = "MXN" });

        Assert.Equal(HttpStatusCode.BadRequest, toJpy.StatusCode);
        Assert.Equal(HttpStatusCode.OK, toMxn.StatusCode);
        Assert.Equal(account with { Currency = "MXN" }, await user.Client.GetFromJsonAsync<AccountView>(url));
    }

    [Fact]
    public async Task Another_Users_Account_is_not_found_for_every_operation()
    {
        var owner = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var account = await CreateAsync(owner, new { name = "Savings", kind = "Savings" });
        var stranger = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var url = $"{AccountsEndpoint}/{account.Id}";

        HttpRequestMessage[] Operations() =>
        [
            new(HttpMethod.Get, url),
            new(HttpMethod.Put, url + "/name") { Content = JsonContent.Create(new { name = "Mine now" }) },
            new(HttpMethod.Put, url + "/currency") { Content = JsonContent.Create(new { currency = "EUR" }) },
        ];

        foreach (var operation in Operations())
        {
            Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.SendAsync(operation)).StatusCode);
        }

        // The routes exist: the owner can do all of it.
        foreach (var operation in Operations())
        {
            Assert.Equal(HttpStatusCode.OK, (await owner.Client.SendAsync(operation)).StatusCode);
        }

        Assert.Empty((await stranger.Client.GetFromJsonAsync<List<AccountView>>(AccountsEndpoint))!);
    }

    private static async Task<AccountView> CreateAsync(SignedInUser user, object account)
    {
        var created = await user.Client.PostAsJsonAsync(AccountsEndpoint, account);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<AccountView>())!;
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record AccountView(
        Guid Id, string Name, string Kind, string Currency, decimal Balance, bool IsLiability, OpeningBalanceView? OpeningBalance);

    private sealed record OpeningBalanceView(decimal Amount, DateOnly Date);
}
