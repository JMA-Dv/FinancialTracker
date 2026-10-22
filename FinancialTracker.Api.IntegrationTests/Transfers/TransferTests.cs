using System.Net;
using System.Net.Http.Json;
using FinancialTracker.Api.IntegrationTests.Infrastructure;

namespace FinancialTracker.Api.IntegrationTests.Transfers;

[Collection(ApiCollection.Name)]
public sealed class TransferTests(ApiFactory api)
{
    private const string TransfersEndpoint = "/api/transfers";
    private const string AccountsEndpoint = "/api/accounts";

    [Fact]
    public async Task A_Transfer_in_one_Currency_lowers_the_from_Account_and_raises_the_to_Account_by_the_same_amount()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", openingBalance: 500m);
        var savings = await OpenAccountAsync(user, "Savings", "Savings", openingBalance: 100m);

        var created = await user.Client.PostAsJsonAsync(TransfersEndpoint, new
        {
            fromAccountId = checking.Id,
            toAccountId = savings.Id,
            date = Today,
            amount = 200.50m,
            note = "Rainy day",
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(299.50m, (await GetAccountAsync(user, checking.Id)).Balance);
        Assert.Equal(300.50m, (await GetAccountAsync(user, savings.Id)).Balance);
    }

    [Fact]
    public async Task A_Transfer_from_Checking_to_a_Credit_Card_reduces_what_is_owed()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", openingBalance: 500m);
        var visa = await OpenAccountAsync(user, "Visa", "CreditCard", openingBalance: -300m);

        var created = await user.Client.PostAsJsonAsync(TransfersEndpoint, New(checking.Id, visa.Id, amount: 120m));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(380m, (await GetAccountAsync(user, checking.Id)).Balance);
        Assert.Equal(-180m, (await GetAccountAsync(user, visa.Id)).Balance);
    }

    public static TheoryData<string, string, Func<Guid, Guid, object>> InvalidTransfers => new()
    {
        { "USD", "the same Account on both sides", (from, _) => New(from, from) },
        { "USD", "a date in the future", (from, to) => New(from, to, date: Today.AddDays(1)) },
        { "USD", "a zero amount", (from, to) => New(from, to, amount: 0m) },
        { "USD", "a negative amount", (from, to) => New(from, to, amount: -5m) },
        { "USD", "more decimal places than USD allows", (from, to) => New(from, to, amount: 10.005m) },
        { "JPY", "decimals in JPY", (from, to) => New(from, to, amount: 10.5m) },
        { "USD", "an amount too large for the database", (from, to) => New(from, to, amount: 1e20m) },
        { "USD", "no amount", (from, to) => new { fromAccountId = from, toAccountId = to, date = Today } },
        { "USD", "no date", (from, to) => new { fromAccountId = from, toAccountId = to, amount = 1m } },
        { "USD", "no from-Account", (_, to) => new { toAccountId = to, date = Today, amount = 1m } },
        { "USD", "no to-Account", (from, _) => new { fromAccountId = from, date = Today, amount = 1m } },
        { "USD", "a note over 500 characters", (from, to) => New(from, to, note: new string('a', 501)) },
    };

    [Theory]
    [MemberData(nameof(InvalidTransfers))]
    public async Task A_Transfer_that_breaks_a_rule_is_rejected_and_the_balances_are_unchanged(
        string currency, string rule, Func<Guid, Guid, object> transfer)
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync(currency);
        var checking = await OpenAccountAsync(user, "Checking", "Checking", openingBalance: 100m);
        var savings = await OpenAccountAsync(user, "Savings", "Savings", openingBalance: 100m);

        var created = await user.Client.PostAsJsonAsync(TransfersEndpoint, transfer(checking.Id, savings.Id));

        Assert.True(created.StatusCode is HttpStatusCode.BadRequest, $"{rule}: got {(int)created.StatusCode}");
        Assert.Equal(100m, (await GetAccountAsync(user, checking.Id)).Balance);
        Assert.Equal(100m, (await GetAccountAsync(user, savings.Id)).Balance);
    }

    [Fact]
    public async Task Using_another_Users_Account_in_a_Transfer_is_not_found()
    {
        var owner = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var ownerAccount = await OpenAccountAsync(owner, "Checking", "Checking", openingBalance: 100m);
        var stranger = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var strangerAccount = await OpenAccountAsync(stranger, "Checking", "Checking", openingBalance: 100m);

        var fromOthers = await stranger.Client.PostAsJsonAsync(TransfersEndpoint, New(ownerAccount.Id, strangerAccount.Id));
        var toOthers = await stranger.Client.PostAsJsonAsync(TransfersEndpoint, New(strangerAccount.Id, ownerAccount.Id));

        Assert.Equal(HttpStatusCode.NotFound, fromOthers.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, toOthers.StatusCode);
        Assert.Equal(100m, (await GetAccountAsync(owner, ownerAccount.Id)).Balance);
        Assert.Equal(100m, (await GetAccountAsync(stranger, strangerAccount.Id)).Balance);
    }

    [Fact]
    public async Task A_Transfer_between_Currencies_moves_the_amount_that_left_and_the_amount_that_arrived()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var dollars = await OpenAccountAsync(user, "Dollars", "Checking", openingBalance: 500m, currency: "USD");
        var euros = await OpenAccountAsync(user, "Euros", "Savings", openingBalance: 10m, currency: "EUR");

        var created = await user.Client.PostAsJsonAsync(TransfersEndpoint, new
        {
            fromAccountId = dollars.Id,
            toAccountId = euros.Id,
            date = Today,
            amount = 100m,
            receivedAmount = 92.40m,
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(400m, (await GetAccountAsync(user, dollars.Id)).Balance);
        Assert.Equal(102.40m, (await GetAccountAsync(user, euros.Id)).Balance);
    }

    public static TheoryData<string, string, string, object?> InvalidCrossCurrencyAmounts => new()
    {
        { "USD", "EUR", "no received amount between Currencies", null },
        { "USD", "EUR", "a zero received amount", new { receivedAmount = 0m } },
        { "USD", "EUR", "a negative received amount", new { receivedAmount = -1m } },
        { "USD", "EUR", "a received amount with more decimals than EUR allows", new { receivedAmount = 92.405m } },
        { "USD", "JPY", "decimals in the received JPY amount", new { receivedAmount = 10.5m } },
        { "USD", "EUR", "a received amount too large for the database", new { receivedAmount = 1e20m } },
        { "USD", "USD", "a received amount different from the amount in one Currency", new { receivedAmount = 99m } },
    };

    [Theory]
    [MemberData(nameof(InvalidCrossCurrencyAmounts))]
    public async Task A_Transfer_whose_received_amount_breaks_a_rule_is_rejected_and_the_balances_are_unchanged(
        string fromCurrency, string toCurrency, string rule, object? received)
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var from = await OpenAccountAsync(user, "From", "Checking", openingBalance: 500m, currency: fromCurrency);
        var to = await OpenAccountAsync(user, "To", "Savings", openingBalance: 10m, currency: toCurrency);
        var receivedAmount = received is null ? null : (decimal?)received.GetType().GetProperty("receivedAmount")!.GetValue(received);

        var created = await user.Client.PostAsJsonAsync(
            TransfersEndpoint, new { fromAccountId = from.Id, toAccountId = to.Id, date = Today, amount = 100m, receivedAmount });

        Assert.True(created.StatusCode is HttpStatusCode.BadRequest, $"{rule}: got {(int)created.StatusCode}");
        Assert.Equal(500m, (await GetAccountAsync(user, from.Id)).Balance);
        Assert.Equal(10m, (await GetAccountAsync(user, to.Id)).Balance);
    }

    [Fact]
    public async Task A_Transfer_in_one_Currency_may_repeat_the_amount_as_the_received_amount()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", openingBalance: 500m);
        var savings = await OpenAccountAsync(user, "Savings", "Savings", openingBalance: 0m);

        var created = await user.Client.PostAsJsonAsync(TransfersEndpoint, new
        {
            fromAccountId = checking.Id,
            toAccountId = savings.Id,
            date = Today,
            amount = 100m,
            receivedAmount = 100m,
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(100m, (await GetAccountAsync(user, savings.Id)).Balance);
    }

    [Fact]
    public async Task A_User_can_get_a_Transfer_with_both_amounts()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var dollars = await OpenAccountAsync(user, "Dollars", "Checking", openingBalance: 500m, currency: "USD");
        var euros = await OpenAccountAsync(user, "Euros", "Savings", openingBalance: 0m, currency: "EUR");
        var id = await RecordAsync(user, new
        {
            fromAccountId = dollars.Id, toAccountId = euros.Id, date = Today.AddDays(-1), amount = 100m, receivedAmount = 92.40m, note = "Trip",
        });

        var transfer = await user.Client.GetFromJsonAsync<TransferView>($"{TransfersEndpoint}/{id}");

        Assert.Equal(new TransferView(id, dollars.Id, euros.Id, Today.AddDays(-1), 100m, 92.40m, "Trip"), transfer);
    }

    [Fact]
    public async Task Transfers_are_listed_newest_first_and_filtered_by_either_side_Account()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", openingBalance: 500m);
        var savings = await OpenAccountAsync(user, "Savings", "Savings", openingBalance: 0m);
        var wallet = await OpenAccountAsync(user, "Wallet", "Cash", openingBalance: 0m);
        var old = await RecordAsync(user, New(checking.Id, savings.Id, date: Today.AddDays(-10)));
        var recent = await RecordAsync(user, New(wallet.Id, checking.Id, date: Today.AddDays(-2)));
        var walletToSavings = await RecordAsync(user, New(wallet.Id, savings.Id, date: Today.AddDays(-5)));
        var other = await api.CreateSignedInUserWithCurrencyAsync("USD");
        await RecordAsync(other, New(
            (await OpenAccountAsync(other, "A", "Checking", 5m)).Id, (await OpenAccountAsync(other, "B", "Savings", 0m)).Id));

        Assert.Equal([recent, walletToSavings, old], await ListAsync(user, ""));
        Assert.Equal([recent, old], await ListAsync(user, $"?accountId={checking.Id}"));
        Assert.Equal([walletToSavings, old], await ListAsync(user, $"?accountId={savings.Id}"));
    }

    [Fact]
    public async Task Editing_a_Transfer_changes_any_field_and_the_balances_follow()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", openingBalance: 500m);
        var savings = await OpenAccountAsync(user, "Savings", "Savings", openingBalance: 100m);
        var wallet = await OpenAccountAsync(user, "Wallet", "Cash", openingBalance: 20m);
        var id = await RecordAsync(user, New(checking.Id, savings.Id, amount: 50m));

        var edited = await user.Client.PutAsJsonAsync($"{TransfersEndpoint}/{id}", new
        {
            fromAccountId = savings.Id, toAccountId = wallet.Id, date = Today.AddDays(-1), amount = 30m, note = "Pocket money",
        });

        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        Assert.Equal(
            new TransferView(id, savings.Id, wallet.Id, Today.AddDays(-1), 30m, 30m, "Pocket money"),
            await edited.Content.ReadFromJsonAsync<TransferView>());
        Assert.Equal(500m, (await GetAccountAsync(user, checking.Id)).Balance);
        Assert.Equal(70m, (await GetAccountAsync(user, savings.Id)).Balance);
        Assert.Equal(50m, (await GetAccountAsync(user, wallet.Id)).Balance);
    }

    [Fact]
    public async Task An_edited_Transfer_that_breaks_a_rule_is_rejected_and_left_as_it_was()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", openingBalance: 500m);
        var savings = await OpenAccountAsync(user, "Savings", "Savings", openingBalance: 100m);
        var euros = await OpenAccountAsync(user, "Euros", "Savings", openingBalance: 0m, currency: "EUR");
        var id = await RecordAsync(user, New(checking.Id, savings.Id, amount: 50m));

        var sameAccount = await user.Client.PutAsJsonAsync($"{TransfersEndpoint}/{id}", New(checking.Id, checking.Id));
        var future = await user.Client.PutAsJsonAsync($"{TransfersEndpoint}/{id}", New(checking.Id, savings.Id, date: Today.AddDays(1)));
        var zero = await user.Client.PutAsJsonAsync($"{TransfersEndpoint}/{id}", New(checking.Id, savings.Id, amount: 0m));
        var missingReceived = await user.Client.PutAsJsonAsync($"{TransfersEndpoint}/{id}", New(checking.Id, euros.Id, amount: 10m));

        Assert.All([sameAccount, future, zero, missingReceived], response => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode));
        Assert.Equal(450m, (await GetAccountAsync(user, checking.Id)).Balance);
        Assert.Equal(150m, (await GetAccountAsync(user, savings.Id)).Balance);
    }

    [Fact]
    public async Task Deleting_a_Transfer_removes_it_and_both_balances_follow()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", openingBalance: 500m);
        var savings = await OpenAccountAsync(user, "Savings", "Savings", openingBalance: 100m);
        var id = await RecordAsync(user, New(checking.Id, savings.Id, amount: 50m));

        var deleted = await user.Client.DeleteAsync($"{TransfersEndpoint}/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await ListAsync(user, ""));
        Assert.Equal(500m, (await GetAccountAsync(user, checking.Id)).Balance);
        Assert.Equal(100m, (await GetAccountAsync(user, savings.Id)).Balance);
        Assert.Equal(HttpStatusCode.NotFound, (await user.Client.DeleteAsync($"{TransfersEndpoint}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await user.Client.GetAsync($"{TransfersEndpoint}/{id}")).StatusCode);
    }

    [Fact]
    public async Task Another_Users_Transfer_is_not_found_to_get_edit_or_delete_and_cant_be_moved_to_their_Account()
    {
        var owner = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var ownerChecking = await OpenAccountAsync(owner, "Checking", "Checking", openingBalance: 500m);
        var ownerSavings = await OpenAccountAsync(owner, "Savings", "Savings", openingBalance: 100m);
        var id = await RecordAsync(owner, New(ownerChecking.Id, ownerSavings.Id, amount: 50m));
        var stranger = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var strangerChecking = await OpenAccountAsync(stranger, "Checking", "Checking", openingBalance: 500m);
        var strangerSavings = await OpenAccountAsync(stranger, "Savings", "Savings", openingBalance: 100m);
        var strangerTransfer = await RecordAsync(stranger, New(strangerChecking.Id, strangerSavings.Id, amount: 5m));

        var get = await stranger.Client.GetAsync($"{TransfersEndpoint}/{id}");
        var edit = await stranger.Client.PutAsJsonAsync($"{TransfersEndpoint}/{id}", New(strangerChecking.Id, strangerSavings.Id));
        var delete = await stranger.Client.DeleteAsync($"{TransfersEndpoint}/{id}");
        var fromOthers = await stranger.Client.PutAsJsonAsync($"{TransfersEndpoint}/{strangerTransfer}", New(ownerChecking.Id, strangerSavings.Id));
        var toOthers = await stranger.Client.PutAsJsonAsync($"{TransfersEndpoint}/{strangerTransfer}", New(strangerChecking.Id, ownerSavings.Id));

        Assert.All([get, edit, delete, fromOthers, toOthers], response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
        Assert.Equal(450m, (await GetAccountAsync(owner, ownerChecking.Id)).Balance);
        Assert.Equal(150m, (await GetAccountAsync(owner, ownerSavings.Id)).Balance);
        Assert.Equal(495m, (await GetAccountAsync(stranger, strangerChecking.Id)).Balance);
    }

    [Fact]
    public async Task An_Account_with_a_Transfer_cant_change_its_Currency()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", openingBalance: 500m);
        var savings = await OpenAccountAsync(user, "Savings", "Savings", openingBalance: 0m);
        await RecordAsync(user, New(checking.Id, savings.Id));

        var fromAccount = await user.Client.PutAsJsonAsync($"{AccountsEndpoint}/{checking.Id}/currency", new { currency = "EUR" });
        var toAccount = await user.Client.PutAsJsonAsync($"{AccountsEndpoint}/{savings.Id}/currency", new { currency = "EUR" });

        Assert.Equal(HttpStatusCode.Conflict, fromAccount.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, toAccount.StatusCode);
    }

    private static async Task<Guid> RecordAsync(SignedInUser user, object transfer)
    {
        var created = await user.Client.PostAsJsonAsync(TransfersEndpoint, transfer);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<TransferView>())!.Id;
    }

    private static async Task<List<Guid>> ListAsync(SignedInUser user, string query) =>
        (await user.Client.GetFromJsonAsync<List<TransferView>>(TransfersEndpoint + query))!.Select(t => t.Id).ToList();

    private static object New(Guid from, Guid to, DateOnly? date = null, decimal amount = 1m, string? note = null) =>
        new { fromAccountId = from, toAccountId = to, date = date ?? Today, amount, note };

    private static async Task<AccountView> OpenAccountAsync(
        SignedInUser user, string name, string kind, decimal openingBalance, string? currency = null)
    {
        var created = await user.Client.PostAsJsonAsync(AccountsEndpoint, new
        {
            name,
            kind,
            currency,
            openingBalance = new { amount = openingBalance, date = Today.AddDays(-30) },
        });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<AccountView>())!;
    }

    private static async Task<AccountView> GetAccountAsync(SignedInUser user, Guid id) =>
        (await user.Client.GetFromJsonAsync<AccountView>($"{AccountsEndpoint}/{id}"))!;

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record TransferView(
        Guid Id, Guid FromAccountId, Guid ToAccountId, DateOnly Date, decimal Amount, decimal ReceivedAmount, string? Note);

    private sealed record AccountView(Guid Id, decimal Balance);
}
