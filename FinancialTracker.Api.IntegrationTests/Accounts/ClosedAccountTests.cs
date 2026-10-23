using System.Net;
using System.Net.Http.Json;
using FinancialTracker.Api.IntegrationTests.Infrastructure;

namespace FinancialTracker.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class ClosedAccountTests(ApiFactory api)
{
    private const string AccountsEndpoint = "/api/accounts";

    [Fact]
    public async Task An_Account_with_a_zero_balance_can_be_closed()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var wallet = await OpenAccountAsync(user, "Wallet", openingBalance: 40m);
        await RecordAsync(user, wallet.Id, Today.AddDays(-1), "Out", 40m);

        var closed = await user.Client.PostAsync($"{AccountsEndpoint}/{wallet.Id}/close", content: null);

        Assert.Equal(HttpStatusCode.OK, closed.StatusCode);
        var account = (await closed.Content.ReadFromJsonAsync<AccountView>())!;
        Assert.True(account.IsClosed);
        Assert.Equal(0m, account.Balance);
        Assert.True((await GetAccountAsync(user, wallet.Id)).IsClosed);
    }

    [Theory]
    [InlineData(40)]
    [InlineData(-40)]
    public async Task An_Account_with_a_non_zero_balance_cant_be_closed(int balance)
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var account = await OpenAccountAsync(user, "Account", openingBalance: balance, kind: balance < 0 ? "CreditCard" : "Checking");

        var closed = await user.Client.PostAsync($"{AccountsEndpoint}/{account.Id}/close", content: null);

        Assert.Equal(HttpStatusCode.Conflict, closed.StatusCode);
        Assert.False((await GetAccountAsync(user, account.Id)).IsClosed);
    }

    [Fact]
    public async Task A_Closed_Account_rejects_new_and_edited_Transactions_Transfers_and_Balance_Adjustments()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var open = await OpenAccountAsync(user, "Checking", openingBalance: 100m);
        var closing = await OpenAccountAsync(user, "Old savings", openingBalance: 25m);
        var spent = await RecordAsync(user, closing.Id, Today.AddDays(-3), "Out", 25m);
        var toOpen = await TransferAsync(user, open.Id, closing.Id, Today.AddDays(-2), 10m);
        var fromClosing = await TransferAsync(user, closing.Id, open.Id, Today.AddDays(-2), 10m);
        var adjustment = await AdjustAsync(user, closing.Id, Today.AddDays(-1), 0m);
        var keptOnOpen = await RecordAsync(user, open.Id, Today.AddDays(-1), "Out", 1m);
        (await user.Client.PostAsync($"{AccountsEndpoint}/{closing.Id}/close", content: null)).EnsureSuccessStatusCode();
        var dining = await DiningAsync(user);

        var responses = new Dictionary<string, HttpResponseMessage>
        {
            ["new Transaction"] = await user.Client.PostAsJsonAsync("/api/transactions", Transaction(closing.Id, dining)),
            ["edit Transaction"] = await user.Client.PutAsJsonAsync($"/api/transactions/{spent}", Transaction(closing.Id, dining)),
            ["move Transaction onto it"] = await user.Client.PutAsJsonAsync($"/api/transactions/{keptOnOpen}", Transaction(closing.Id, dining)),
            ["move Transaction off it"] = await user.Client.PutAsJsonAsync($"/api/transactions/{spent}", Transaction(open.Id, dining)),
            ["delete Transaction"] = await user.Client.DeleteAsync($"/api/transactions/{spent}"),
            ["new Transfer in"] = await user.Client.PostAsJsonAsync("/api/transfers", Transfer(open.Id, closing.Id)),
            ["new Transfer out"] = await user.Client.PostAsJsonAsync("/api/transfers", Transfer(closing.Id, open.Id)),
            ["edit Transfer"] = await user.Client.PutAsJsonAsync($"/api/transfers/{toOpen}", Transfer(open.Id, closing.Id)),
            ["move Transfer off it"] = await user.Client.PutAsJsonAsync($"/api/transfers/{fromClosing}", Transfer(open.Id, open.Id)),
            ["delete Transfer"] = await user.Client.DeleteAsync($"/api/transfers/{toOpen}"),
            ["new Balance Adjustment"] = await user.Client.PostAsJsonAsync(AdjustmentsOf(closing.Id), new { date = Today, balance = 5m }),
            ["edit Balance Adjustment"] = await user.Client.PutAsJsonAsync(
                $"{AdjustmentsOf(closing.Id)}/{adjustment.Id}", new { date = Today, balance = 5m }),
            ["delete Balance Adjustment"] = await user.Client.DeleteAsync($"{AdjustmentsOf(closing.Id)}/{adjustment.Id}"),
        };

        foreach (var (what, response) in responses)
        {
            Assert.True(response.StatusCode is HttpStatusCode.Conflict, $"{what}: got {(int)response.StatusCode}");
        }

        Assert.Equal(0m, (await GetAccountAsync(user, closing.Id)).Balance);
        Assert.Equal(99m, (await GetAccountAsync(user, open.Id)).Balance);
    }

    [Fact]
    public async Task A_Closed_Accounts_history_and_balance_on_past_dates_stay_readable()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var wallet = await OpenAccountAsync(user, "Wallet", openingBalance: 40m);
        var spent = await RecordAsync(user, wallet.Id, Today.AddDays(-5), "Out", 40m);
        (await user.Client.PostAsync($"{AccountsEndpoint}/{wallet.Id}/close", content: null)).EnsureSuccessStatusCode();

        var before = await user.Client.GetFromJsonAsync<BalanceView>(
            $"{AccountsEndpoint}/{wallet.Id}/balance?date={Today.AddDays(-6):yyyy-MM-dd}");
        var history = await user.Client.GetFromJsonAsync<List<HistoryEntryView>>($"{AccountsEndpoint}/{wallet.Id}/history");
        var adjustments = await user.Client.GetAsync(AdjustmentsOf(wallet.Id));

        Assert.Equal(40m, before!.Balance);
        Assert.Equal(2, history!.Count);
        Assert.Contains(history, entry => entry.Id == spent);
        Assert.Equal(HttpStatusCode.OK, adjustments.StatusCode);
    }

    [Fact]
    public async Task The_Account_list_hides_Closed_Accounts_unless_asked_to_include_them()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var kept = await OpenAccountAsync(user, "Kept", openingBalance: 10m);
        var old = await OpenAccountAsync(user, "Old", openingBalance: 0m);
        (await user.Client.PostAsync($"{AccountsEndpoint}/{old.Id}/close", content: null)).EnsureSuccessStatusCode();

        var hidden = await user.Client.GetFromJsonAsync<List<AccountView>>(AccountsEndpoint);
        var all = await user.Client.GetFromJsonAsync<List<AccountView>>($"{AccountsEndpoint}?includeClosed=true");

        Assert.Equal([kept.Id], hidden!.Select(a => a.Id));
        Assert.Equal([kept.Id, old.Id], all!.Select(a => a.Id));
        Assert.Equal(HttpStatusCode.OK, (await user.Client.GetAsync($"{AccountsEndpoint}/{old.Id}")).StatusCode);
    }

    [Fact]
    public async Task A_Closed_Account_can_be_reopened_and_recorded_against_again()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var wallet = await OpenAccountAsync(user, "Wallet", openingBalance: 0m);
        (await user.Client.PostAsync($"{AccountsEndpoint}/{wallet.Id}/close", content: null)).EnsureSuccessStatusCode();

        var reopened = await user.Client.PostAsync($"{AccountsEndpoint}/{wallet.Id}/reopen", content: null);

        Assert.Equal(HttpStatusCode.OK, reopened.StatusCode);
        Assert.False((await reopened.Content.ReadFromJsonAsync<AccountView>())!.IsClosed);
        await RecordAsync(user, wallet.Id, Today, "In", 12m);
        Assert.Equal(12m, (await GetAccountAsync(user, wallet.Id)).Balance);
        Assert.Contains(wallet.Id, (await user.Client.GetFromJsonAsync<List<AccountView>>(AccountsEndpoint))!.Select(a => a.Id));
    }

    [Fact]
    public async Task Another_Users_Account_cant_be_closed_or_reopened()
    {
        var owner = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var wallet = await OpenAccountAsync(owner, "Wallet", openingBalance: 0m);
        var stranger = await api.CreateSignedInUserWithCurrencyAsync("USD");

        var close = await stranger.Client.PostAsync($"{AccountsEndpoint}/{wallet.Id}/close", content: null);
        var reopen = await stranger.Client.PostAsync($"{AccountsEndpoint}/{wallet.Id}/reopen", content: null);

        Assert.Equal(HttpStatusCode.NotFound, close.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, reopen.StatusCode);
        Assert.False((await GetAccountAsync(owner, wallet.Id)).IsClosed);
    }

    private static string AdjustmentsOf(Guid accountId) => $"{AccountsEndpoint}/{accountId}/balance-adjustments";

    private static object Transaction(Guid account, Guid category) =>
        new { accountId = account, categoryId = category, date = Today, direction = "In", amount = 1m };

    private static object Transfer(Guid from, Guid to) => new { fromAccountId = from, toAccountId = to, date = Today, amount = 1m };

    private static async Task<AdjustmentView> AdjustAsync(SignedInUser user, Guid accountId, DateOnly date, decimal balance)
    {
        var created = await user.Client.PostAsJsonAsync(AdjustmentsOf(accountId), new { date, balance });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<AdjustmentView>())!;
    }

    private static async Task<Guid> DiningAsync(SignedInUser user)
    {
        var tree = await user.Client.GetFromJsonAsync<List<CategoryView>>("/api/categories");
        return Assert.Single(tree!, node => node.Name == "Dining").Id;
    }

    private static async Task<Guid> RecordAsync(SignedInUser user, Guid accountId, DateOnly date, string direction, decimal amount)
    {
        var created = await user.Client.PostAsJsonAsync(
            "/api/transactions", new { accountId, date, direction, amount, categoryId = await DiningAsync(user) });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<IdView>())!.Id;
    }

    private static async Task<Guid> TransferAsync(SignedInUser user, Guid from, Guid to, DateOnly date, decimal amount)
    {
        var created = await user.Client.PostAsJsonAsync(
            "/api/transfers", new { fromAccountId = from, toAccountId = to, date, amount });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<IdView>())!.Id;
    }

    private static async Task<AccountView> OpenAccountAsync(
        SignedInUser user, string name, decimal openingBalance, string kind = "Checking")
    {
        var created = await user.Client.PostAsJsonAsync(AccountsEndpoint, new
        {
            name,
            kind,
            openingBalance = new { amount = openingBalance, date = Today.AddDays(-30) },
        });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<AccountView>())!;
    }

    private static async Task<AccountView> GetAccountAsync(SignedInUser user, Guid id) =>
        (await user.Client.GetFromJsonAsync<AccountView>($"{AccountsEndpoint}/{id}"))!;

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record AccountView(Guid Id, decimal Balance, bool IsClosed);

    private sealed record AdjustmentView(Guid Id);

    private sealed record BalanceView(DateOnly Date, decimal Balance);

    private sealed record HistoryEntryView(string Kind, Guid Id);

    private sealed record IdView(Guid Id);

    private sealed record CategoryView(Guid Id, string Name);
}
