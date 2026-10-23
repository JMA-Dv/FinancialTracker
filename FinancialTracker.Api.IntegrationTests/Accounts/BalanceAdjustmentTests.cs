using System.Net;
using System.Net.Http.Json;
using FinancialTracker.Api.IntegrationTests.Infrastructure;

namespace FinancialTracker.Api.IntegrationTests.Accounts;

[Collection(ApiCollection.Name)]
public sealed class BalanceAdjustmentTests(ApiFactory api)
{
    private const string AccountsEndpoint = "/api/accounts";

    [Fact]
    public async Task A_Balance_Adjustment_matches_the_Account_to_the_stated_balance_and_shows_the_difference()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        await RecordAsync(user, checking.Id, Today.AddDays(-10), "Out", 30m);

        var created = await user.Client.PostAsJsonAsync(AdjustmentsOf(checking.Id), new { date = Today.AddDays(-5), balance = 90m });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var adjustment = (await created.Content.ReadFromJsonAsync<AdjustmentView>())!;
        Assert.Equal(90m, adjustment.Balance);
        Assert.Equal(20m, adjustment.Difference);
        Assert.Equal(90m, (await GetAccountAsync(user, checking.Id)).Balance);
        Assert.Equal(adjustment, await user.Client.GetFromJsonAsync<AdjustmentView>($"{AdjustmentsOf(checking.Id)}/{adjustment.Id}"));
    }

    [Fact]
    public async Task A_Balance_Adjustment_is_never_income_or_spending()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);

        (await user.Client.PostAsJsonAsync(AdjustmentsOf(checking.Id), new { date = Today, balance = 500m })).EnsureSuccessStatusCode();

        Assert.Empty((await user.Client.GetFromJsonAsync<List<TransactionView>>("/api/transactions"))!);
    }

    [Fact]
    public async Task A_Balance_Adjustment_dated_today_is_accepted_and_one_in_the_future_is_rejected()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);

        var today = await user.Client.PostAsJsonAsync(AdjustmentsOf(checking.Id), new { date = Today, balance = 120m });
        var future = await user.Client.PostAsJsonAsync(AdjustmentsOf(checking.Id), new { date = Today.AddDays(1), balance = 999m });

        Assert.Equal(HttpStatusCode.Created, today.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
        Assert.Equal(120m, (await GetAccountAsync(user, checking.Id)).Balance);
    }

    public static TheoryData<string, string, object> InvalidAdjustments => new()
    {
        { "USD", "more decimal places than USD allows", new { date = Today, balance = 10.005m } },
        { "JPY", "decimals in JPY", new { date = Today, balance = 10.5m } },
        { "USD", "a balance too large for the database", new { date = Today, balance = 1e20m } },
        { "USD", "no balance", new { date = Today } },
        { "USD", "no date", new { balance = 10m } },
        { "USD", "the date of an existing Balance Adjustment", new { date = Today.AddDays(-30), balance = 10m } },
    };

    [Theory]
    [MemberData(nameof(InvalidAdjustments))]
    public async Task A_Balance_Adjustment_that_breaks_a_rule_is_rejected_and_the_balance_is_unchanged(
        string currency, string rule, object adjustment)
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync(currency);
        var checking = await OpenAccountAsync(user, openingBalance: 100m);

        var created = await user.Client.PostAsJsonAsync(AdjustmentsOf(checking.Id), adjustment);

        Assert.True(
            created.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict, $"{rule}: got {(int)created.StatusCode}");
        Assert.Equal(100m, (await GetAccountAsync(user, checking.Id)).Balance);
    }

    [Fact]
    public async Task An_Opening_Balance_can_be_added_later_to_an_Account_created_without_one()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var wallet = await OpenAccountAsync(user, openingBalance: null);
        await RecordAsync(user, wallet.Id, Today.AddDays(-2), "Out", 15m);

        await AdjustAsync(user, wallet.Id, Today.AddDays(-20), 200m);

        var account = await GetAccountAsync(user, wallet.Id);
        Assert.Equal(185m, account.Balance);
        Assert.Equal(new OpeningBalanceView(200m, Today.AddDays(-20)), account.OpeningBalance);
    }

    [Fact]
    public async Task The_Opening_Balance_can_be_edited_and_deleted_like_any_Balance_Adjustment()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        await RecordAsync(user, checking.Id, Today.AddDays(-2), "Out", 15m);
        var opening = Assert.Single(await ListAdjustmentsAsync(user, checking.Id));

        var edited = await user.Client.PutAsJsonAsync(
            $"{AdjustmentsOf(checking.Id)}/{opening.Id}", new { date = Today.AddDays(-25), balance = 300m });

        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        var afterEdit = await GetAccountAsync(user, checking.Id);
        Assert.Equal(285m, afterEdit.Balance);
        Assert.Equal(new OpeningBalanceView(300m, Today.AddDays(-25)), afterEdit.OpeningBalance);

        var deleted = await user.Client.DeleteAsync($"{AdjustmentsOf(checking.Id)}/{opening.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        var afterDelete = await GetAccountAsync(user, checking.Id);
        Assert.Equal(-15m, afterDelete.Balance);
        Assert.Null(afterDelete.OpeningBalance);
        Assert.Empty(await ListAdjustmentsAsync(user, checking.Id));
        Assert.Equal(HttpStatusCode.NotFound, (await user.Client.DeleteAsync($"{AdjustmentsOf(checking.Id)}/{opening.Id}")).StatusCode);
    }

    [Fact]
    public async Task An_edit_that_breaks_a_rule_is_rejected_and_changes_nothing()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var later = await AdjustAsync(user, checking.Id, Today.AddDays(-5), 150m);

        var intoFuture = await user.Client.PutAsJsonAsync(
            $"{AdjustmentsOf(checking.Id)}/{later.Id}", new { date = Today.AddDays(1), balance = 999m });
        var ontoOpening = await user.Client.PutAsJsonAsync(
            $"{AdjustmentsOf(checking.Id)}/{later.Id}", new { date = Today.AddDays(-30), balance = 999m });

        Assert.Equal(HttpStatusCode.BadRequest, intoFuture.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, ontoOpening.StatusCode);
        Assert.Equal(150m, (await GetAccountAsync(user, checking.Id)).Balance);
    }

    [Fact]
    public async Task Each_Balance_Adjustment_shows_the_difference_it_made()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        await RecordAsync(user, checking.Id, Today.AddDays(-10), "Out", 30m);
        var first = await AdjustAsync(user, checking.Id, Today.AddDays(-8), 90m);
        await RecordAsync(user, checking.Id, Today.AddDays(-5), "Out", 10m);
        var second = await AdjustAsync(user, checking.Id, Today.AddDays(-3), 70m);

        var listed = await ListAdjustmentsAsync(user, checking.Id);

        Assert.Equal(20m, first.Difference);
        Assert.Equal(-10m, second.Difference);
        Assert.Equal([second.Id, first.Id], listed.Where(a => a.Date > Today.AddDays(-30)).Select(a => a.Id));
        Assert.Equal(-10m, listed.Single(a => a.Id == second.Id).Difference);
    }

    [Fact]
    public async Task Editing_a_Balance_Adjustment_updates_the_difference_it_shows()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var adjustment = await AdjustAsync(user, checking.Id, Today.AddDays(-5), 90m);

        var edited = await user.Client.PutAsJsonAsync(
            $"{AdjustmentsOf(checking.Id)}/{adjustment.Id}", new { date = Today.AddDays(-5), balance = 130m });

        Assert.Equal(30m, (await edited.Content.ReadFromJsonAsync<AdjustmentView>())!.Difference);
    }

    [Fact]
    public async Task A_Transaction_dated_the_same_day_as_a_Balance_Adjustment_is_considered_included_in_it()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        await AdjustAsync(user, checking.Id, Today.AddDays(-5), 400m);

        await RecordAsync(user, checking.Id, Today.AddDays(-5), "Out", 25m);
        await RecordAsync(user, checking.Id, Today.AddDays(-4), "Out", 10m);

        Assert.Equal(390m, (await GetAccountAsync(user, checking.Id)).Balance);
        Assert.Equal(400m, await BalanceOnAsync(user, checking.Id, Today.AddDays(-5)));
    }

    [Fact]
    public async Task A_back_dated_Transaction_entered_before_a_later_Balance_Adjustment_leaves_the_adjusted_balance_unchanged()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        await AdjustAsync(user, checking.Id, Today.AddDays(-5), 400m);

        await RecordAsync(user, checking.Id, Today.AddDays(-10), "Out", 60m);

        Assert.Equal(400m, (await GetAccountAsync(user, checking.Id)).Balance);
        Assert.Equal(40m, await BalanceOnAsync(user, checking.Id, Today.AddDays(-7)));
    }

    [Fact]
    public async Task The_balance_on_a_past_date_is_the_latest_adjustment_on_or_before_it_plus_money_moved_since()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var savings = await OpenAccountAsync(user, openingBalance: 0m);
        await RecordAsync(user, checking.Id, Today.AddDays(-20), "Out", 30m);
        await AdjustAsync(user, checking.Id, Today.AddDays(-15), 90m);
        await RecordAsync(user, checking.Id, Today.AddDays(-10), "In", 50m);
        (await user.Client.PostAsJsonAsync("/api/transfers", new
        {
            fromAccountId = checking.Id,
            toAccountId = savings.Id,
            date = Today.AddDays(-8),
            amount = 40m,
        })).EnsureSuccessStatusCode();

        Assert.Equal(100m, await BalanceOnAsync(user, checking.Id, Today.AddDays(-30)));
        Assert.Equal(70m, await BalanceOnAsync(user, checking.Id, Today.AddDays(-16)));
        Assert.Equal(90m, await BalanceOnAsync(user, checking.Id, Today.AddDays(-15)));
        Assert.Equal(140m, await BalanceOnAsync(user, checking.Id, Today.AddDays(-10)));
        Assert.Equal(100m, await BalanceOnAsync(user, checking.Id, Today.AddDays(-8)));
        Assert.Equal(100m, await BalanceOnAsync(user, checking.Id, Today));
    }

    [Fact]
    public async Task The_balance_on_a_future_date_or_without_a_date_is_rejected()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);

        var future = await user.Client.GetAsync($"{AccountsEndpoint}/{checking.Id}/balance?date={Today.AddDays(1):yyyy-MM-dd}");
        var missing = await user.Client.GetAsync($"{AccountsEndpoint}/{checking.Id}/balance");

        Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
    }

    [Fact]
    public async Task Account_history_lists_Transactions_Transfers_and_Balance_Adjustments_newest_first()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var savings = await OpenAccountAsync(user, openingBalance: 0m);
        var lunch = await RecordAsync(user, checking.Id, Today.AddDays(-10), "Out", 30m);
        var adjustment = await AdjustAsync(user, checking.Id, Today.AddDays(-8), 90m);
        var transfer = await TransferAsync(user, checking.Id, savings.Id, Today.AddDays(-6), 40m);
        var paycheck = await RecordAsync(user, checking.Id, Today.AddDays(-2), "In", 50m);
        await RecordAsync(user, savings.Id, Today.AddDays(-1), "Out", 1m);

        var history = (await user.Client.GetFromJsonAsync<List<HistoryEntryView>>($"{AccountsEndpoint}/{checking.Id}/history"))!;

        Assert.Equal(
            [
                new HistoryEntryView("Transaction", paycheck, Today.AddDays(-2), 50m, null),
                new HistoryEntryView("Transfer", transfer, Today.AddDays(-6), -40m, null),
                new HistoryEntryView("BalanceAdjustment", adjustment.Id, Today.AddDays(-8), 20m, 90m),
                new HistoryEntryView("Transaction", lunch, Today.AddDays(-10), -30m, null),
                new HistoryEntryView("BalanceAdjustment", history[^1].Id, Today.AddDays(-30), 100m, 100m),
            ],
            history);
    }

    [Fact]
    public async Task Account_history_puts_a_Balance_Adjustment_after_the_Transactions_of_its_day()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var wallet = await OpenAccountAsync(user, openingBalance: null);
        var adjustment = await AdjustAsync(user, wallet.Id, Today.AddDays(-3), 80m);
        var sameDay = await RecordAsync(user, wallet.Id, Today.AddDays(-3), "Out", 5m);

        var history = (await user.Client.GetFromJsonAsync<List<HistoryEntryView>>($"{AccountsEndpoint}/{wallet.Id}/history"))!;

        // Newest first, so the end-of-day statement comes before the day's Transactions.
        Assert.Equal([adjustment.Id, sameDay], history.Select(entry => entry.Id));
    }

    [Fact]
    public async Task Another_Users_Balance_Adjustments_and_Accounts_are_not_found()
    {
        var owner = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var ownerChecking = await OpenAccountAsync(owner, openingBalance: 100m);
        var ownerAdjustment = await AdjustAsync(owner, ownerChecking.Id, Today.AddDays(-5), 150m);
        var stranger = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var strangerChecking = await OpenAccountAsync(stranger, openingBalance: 100m);
        var strangerAdjustment = await AdjustAsync(stranger, strangerChecking.Id, Today.AddDays(-5), 150m);
        var theirs = $"{AdjustmentsOf(ownerChecking.Id)}/{ownerAdjustment.Id}";
        var body = new { date = Today.AddDays(-4), balance = 1m };

        var responses = new[]
        {
            await stranger.Client.GetAsync(theirs),
            await stranger.Client.PutAsJsonAsync(theirs, body),
            await stranger.Client.DeleteAsync(theirs),
            await stranger.Client.GetAsync(AdjustmentsOf(ownerChecking.Id)),
            await stranger.Client.PostAsJsonAsync(AdjustmentsOf(ownerChecking.Id), body),
            await stranger.Client.GetAsync($"{AccountsEndpoint}/{ownerChecking.Id}/balance?date={Today:yyyy-MM-dd}"),
            await stranger.Client.GetAsync($"{AccountsEndpoint}/{ownerChecking.Id}/history"),
            // Their own Account, someone else's Balance Adjustment.
            await stranger.Client.GetAsync($"{AdjustmentsOf(strangerChecking.Id)}/{ownerAdjustment.Id}"),
            await stranger.Client.DeleteAsync($"{AdjustmentsOf(strangerChecking.Id)}/{ownerAdjustment.Id}"),
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
        Assert.Equal(150m, (await GetAccountAsync(owner, ownerChecking.Id)).Balance);
        Assert.Equal(150m, (await GetAccountAsync(stranger, strangerChecking.Id)).Balance);
        Assert.Equal(HttpStatusCode.OK, (await stranger.Client.GetAsync($"{AdjustmentsOf(strangerChecking.Id)}/{strangerAdjustment.Id}")).StatusCode);
    }

    private static string AdjustmentsOf(Guid accountId) => $"{AccountsEndpoint}/{accountId}/balance-adjustments";

    private static async Task<AdjustmentView> AdjustAsync(SignedInUser user, Guid accountId, DateOnly date, decimal balance)
    {
        var created = await user.Client.PostAsJsonAsync(AdjustmentsOf(accountId), new { date, balance });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<AdjustmentView>())!;
    }

    private static async Task<List<AdjustmentView>> ListAdjustmentsAsync(SignedInUser user, Guid accountId) =>
        (await user.Client.GetFromJsonAsync<List<AdjustmentView>>(AdjustmentsOf(accountId)))!;

    private static async Task<decimal> BalanceOnAsync(SignedInUser user, Guid accountId, DateOnly date) =>
        (await user.Client.GetFromJsonAsync<BalanceView>($"{AccountsEndpoint}/{accountId}/balance?date={date:yyyy-MM-dd}"))!.Balance;

    private static async Task<Guid> RecordAsync(SignedInUser user, Guid accountId, DateOnly date, string direction, decimal amount)
    {
        var tree = await user.Client.GetFromJsonAsync<List<CategoryView>>("/api/categories");
        var dining = Assert.Single(tree!, node => node.Name == "Dining");
        var created = await user.Client.PostAsJsonAsync(
            "/api/transactions", new { accountId, date, direction, amount, categoryId = dining.Id });
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

    private static async Task<AccountView> OpenAccountAsync(SignedInUser user, decimal? openingBalance)
    {
        var created = await user.Client.PostAsJsonAsync(AccountsEndpoint, new
        {
            name = "Checking",
            kind = "Checking",
            openingBalance = openingBalance is { } amount ? new { amount, date = Today.AddDays(-30) } : null,
        });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<AccountView>())!;
    }

    private static async Task<AccountView> GetAccountAsync(SignedInUser user, Guid id) =>
        (await user.Client.GetFromJsonAsync<AccountView>($"{AccountsEndpoint}/{id}"))!;

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record AdjustmentView(Guid Id, DateOnly Date, decimal Balance, decimal Difference);

    private sealed record HistoryEntryView(string Kind, Guid Id, DateOnly Date, decimal Amount, decimal? StatedBalance);

    private sealed record BalanceView(DateOnly Date, decimal Balance);

    private sealed record OpeningBalanceView(decimal Amount, DateOnly Date);

    private sealed record AccountView(Guid Id, decimal Balance, OpeningBalanceView? OpeningBalance);

    private sealed record TransactionView(Guid Id);

    private sealed record IdView(Guid Id);

    private sealed record CategoryView(Guid Id, string Name);
}
