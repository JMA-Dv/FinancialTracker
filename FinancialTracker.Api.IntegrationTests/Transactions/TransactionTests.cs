using System.Net;
using System.Net.Http.Json;
using FinancialTracker.Api.IntegrationTests.Infrastructure;

namespace FinancialTracker.Api.IntegrationTests.Transactions;

[Collection(ApiCollection.Name)]
public sealed class TransactionTests(ApiFactory api)
{
    private const string TransactionsEndpoint = "/api/transactions";
    private const string AccountsEndpoint = "/api/accounts";
    private const string CategoriesEndpoint = "/api/categories";

    [Fact]
    public async Task An_Out_Transaction_reduces_the_Accounts_balance()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var dining = await CategoryAsync(user, "Dining");

        var created = await user.Client.PostAsJsonAsync(TransactionsEndpoint, new
        {
            accountId = checking.Id,
            date = Today,
            direction = "Out",
            amount = 30.25m,
            categoryId = dining.Id,
            note = "Lunch",
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(69.75m, (await GetAccountAsync(user, checking.Id)).Balance);
    }

    [Fact]
    public async Task An_In_Transaction_in_an_Expense_Category_is_a_Refund_that_raises_the_balance()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var shopping = await CategoryAsync(user, "Shopping");

        var created = await user.Client.PostAsJsonAsync(TransactionsEndpoint, new
        {
            accountId = checking.Id,
            date = Today,
            direction = "In",
            amount = 20m,
            categoryId = shopping.Id,
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(120m, (await GetAccountAsync(user, checking.Id)).Balance);
    }

    public static TheoryData<string, string, Func<Guid, Guid, object>> InvalidTransactions => new()
    {
        { "USD", "a date in the future", (account, category) => New(account, category, date: Today.AddDays(1)) },
        { "USD", "a zero amount", (account, category) => New(account, category, amount: 0m) },
        { "USD", "a negative amount", (account, category) => New(account, category, amount: -5m) },
        { "USD", "more decimal places than USD allows", (account, category) => New(account, category, amount: 10.005m) },
        { "JPY", "decimals in JPY", (account, category) => New(account, category, amount: 10.5m) },
        { "USD", "an amount too large for the database", (account, category) => New(account, category, amount: 1e20m) },
        { "USD", "no amount", (account, category) => new { accountId = account, categoryId = category, date = Today, direction = "Out" } },
        { "USD", "no date", (account, category) => new { accountId = account, categoryId = category, direction = "Out", amount = 1m } },
        { "USD", "no direction", (account, category) => new { accountId = account, categoryId = category, date = Today, amount = 1m } },
        { "USD", "a direction that doesn't exist", (account, category) => New(account, category, direction: 99) },
        { "USD", "a note over 500 characters", (account, category) => New(account, category, note: new string('a', 501)) },
    };

    [Theory]
    [MemberData(nameof(InvalidTransactions))]
    public async Task A_Transaction_that_breaks_a_rule_is_rejected_and_the_balance_is_unchanged(
        string currency, string rule, Func<Guid, Guid, object> transaction)
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync(currency);
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var dining = await CategoryAsync(user, "Dining");

        var created = await user.Client.PostAsJsonAsync(TransactionsEndpoint, transaction(checking.Id, dining.Id));

        Assert.True(created.StatusCode is HttpStatusCode.BadRequest, $"{rule}: got {(int)created.StatusCode}");
        Assert.Equal(100m, (await GetAccountAsync(user, checking.Id)).Balance);
    }

    [Fact]
    public async Task Another_Users_Account_or_Category_is_not_found_when_recording_a_Transaction()
    {
        var owner = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var ownerAccount = await OpenAccountAsync(owner, openingBalance: 100m);
        var ownerCategory = await CategoryAsync(owner, "Dining");
        var stranger = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var strangerAccount = await OpenAccountAsync(stranger, openingBalance: 100m);
        var strangerCategory = await CategoryAsync(stranger, "Dining");

        var onOthersAccount = await stranger.Client.PostAsJsonAsync(TransactionsEndpoint, New(ownerAccount.Id, strangerCategory.Id));
        var inOthersCategory = await stranger.Client.PostAsJsonAsync(TransactionsEndpoint, New(strangerAccount.Id, ownerCategory.Id));

        Assert.Equal(HttpStatusCode.NotFound, onOthersAccount.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, inOthersCategory.StatusCode);
        Assert.Equal(100m, (await GetAccountAsync(owner, ownerAccount.Id)).Balance);
        Assert.Equal(100m, (await GetAccountAsync(stranger, strangerAccount.Id)).Balance);
    }

    [Fact]
    public async Task A_new_Transaction_cant_use_an_Archived_Category()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var dining = await CategoryAsync(user, "Dining");
        (await user.Client.PostAsync($"{CategoriesEndpoint}/{dining.Id}/archive", content: null)).EnsureSuccessStatusCode();

        var created = await user.Client.PostAsJsonAsync(TransactionsEndpoint, New(checking.Id, dining.Id));

        Assert.Equal(HttpStatusCode.Conflict, created.StatusCode);
        Assert.Equal(100m, (await GetAccountAsync(user, checking.Id)).Balance);
    }

    [Fact]
    public async Task A_Transaction_can_use_a_parent_Category_directly_and_an_Out_Transaction_can_use_an_Income_Category()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var health = await CategoryAsync(user, "Health");
        (await user.Client.PostAsJsonAsync(CategoriesEndpoint, new { name = "Dentist", parentId = health.Id })).EnsureSuccessStatusCode();
        var income = await CategoryAsync(user, "Income");

        var underParent = await user.Client.PostAsJsonAsync(TransactionsEndpoint, New(checking.Id, health.Id, amount: 10m));
        var outOfIncome = await user.Client.PostAsJsonAsync(TransactionsEndpoint, New(checking.Id, income.Id, amount: 5m));

        Assert.Equal(HttpStatusCode.Created, underParent.StatusCode);
        Assert.Equal(HttpStatusCode.Created, outOfIncome.StatusCode);
        Assert.Equal(85m, (await GetAccountAsync(user, checking.Id)).Balance);
    }

    [Fact]
    public async Task Transactions_are_listed_newest_first_and_filtered_by_Account_Category_and_date_range()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 0m);
        var wallet = await OpenAccountAsync(user, openingBalance: 0m);
        var dining = await CategoryAsync(user, "Dining");
        var gifts = await CategoryAsync(user, "Gifts");
        var oldDinner = await RecordAsync(user, New(checking.Id, dining.Id, date: Today.AddDays(-10), amount: 1m));
        var recentGift = await RecordAsync(user, New(checking.Id, gifts.Id, date: Today.AddDays(-2), amount: 2m));
        var walletDinner = await RecordAsync(user, New(wallet.Id, dining.Id, date: Today.AddDays(-5), amount: 3m));
        var other = await api.CreateSignedInUserWithCurrencyAsync("USD");
        await RecordAsync(other, New((await OpenAccountAsync(other, 0m)).Id, (await CategoryAsync(other, "Dining")).Id));

        Assert.Equal([recentGift, walletDinner, oldDinner], await ListAsync(user, ""));
        Assert.Equal([recentGift, oldDinner], await ListAsync(user, $"?accountId={checking.Id}"));
        Assert.Equal([walletDinner, oldDinner], await ListAsync(user, $"?categoryId={dining.Id}"));
        Assert.Equal([walletDinner], await ListAsync(user, $"?from={Today.AddDays(-6)}&to={Today.AddDays(-3)}"));
        Assert.Equal([walletDinner, oldDinner], await ListAsync(user, $"?to={Today.AddDays(-5)}"));
        Assert.Equal([walletDinner], await ListAsync(user, $"?accountId={wallet.Id}&categoryId={dining.Id}&from={Today.AddDays(-5)}"));
    }

    [Fact]
    public async Task Editing_a_Transaction_changes_any_field_and_the_balances_follow()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var wallet = await OpenAccountAsync(user, openingBalance: 50m);
        var dining = await CategoryAsync(user, "Dining");
        var gifts = await CategoryAsync(user, "Gifts");
        var id = await RecordAsync(user, New(checking.Id, dining.Id, amount: 30m));

        var edited = await user.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{id}", new
        {
            accountId = wallet.Id,
            categoryId = gifts.Id,
            date = Today.AddDays(-1),
            direction = "In",
            amount = 12.5m,
            note = "Birthday",
        });

        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        var view = (await edited.Content.ReadFromJsonAsync<TransactionView>())!;
        Assert.Equal(new TransactionView(id, wallet.Id, Today.AddDays(-1), "In", 12.5m, gifts.Id, "Birthday"), view);
        Assert.Equal(100m, (await GetAccountAsync(user, checking.Id)).Balance);
        Assert.Equal(62.5m, (await GetAccountAsync(user, wallet.Id)).Balance);
    }

    [Fact]
    public async Task Deleting_a_Transaction_removes_it_and_the_balance_follows()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var id = await RecordAsync(user, New(checking.Id, (await CategoryAsync(user, "Dining")).Id, amount: 30m));

        var deleted = await user.Client.DeleteAsync($"{TransactionsEndpoint}/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await ListAsync(user, ""));
        Assert.Equal(100m, (await GetAccountAsync(user, checking.Id)).Balance);
        Assert.Equal(HttpStatusCode.NotFound, (await user.Client.DeleteAsync($"{TransactionsEndpoint}/{id}")).StatusCode);
    }

    [Fact]
    public async Task An_edited_Transaction_that_breaks_a_rule_is_rejected_and_left_as_it_was()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var dining = await CategoryAsync(user, "Dining");
        var id = await RecordAsync(user, New(checking.Id, dining.Id, amount: 30m));

        var future = await user.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{id}", New(checking.Id, dining.Id, date: Today.AddDays(1)));
        var zero = await user.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{id}", New(checking.Id, dining.Id, amount: 0m));
        var tooPrecise = await user.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{id}", New(checking.Id, dining.Id, amount: 10.005m));

        Assert.All([future, zero, tooPrecise], response => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode));
        Assert.Equal(70m, (await GetAccountAsync(user, checking.Id)).Balance);
    }

    [Fact]
    public async Task A_Transaction_cant_move_to_an_Account_in_another_Currency()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var euros = await OpenAccountAsync(user, openingBalance: 100m, currency: "EUR");
        var dining = await CategoryAsync(user, "Dining");
        var id = await RecordAsync(user, New(checking.Id, dining.Id, amount: 30m));

        var moved = await user.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{id}", New(euros.Id, dining.Id, amount: 30m));

        Assert.Equal(HttpStatusCode.BadRequest, moved.StatusCode);
        Assert.Equal(70m, (await GetAccountAsync(user, checking.Id)).Balance);
        Assert.Equal(100m, (await GetAccountAsync(user, euros.Id)).Balance);
    }

    [Fact]
    public async Task An_existing_Transaction_keeps_its_Archived_Category_when_edited_but_cant_switch_to_one()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        var dining = await CategoryAsync(user, "Dining");
        var gifts = await CategoryAsync(user, "Gifts");
        var id = await RecordAsync(user, New(checking.Id, dining.Id, amount: 30m));
        (await user.Client.PostAsync($"{CategoriesEndpoint}/{dining.Id}/archive", content: null)).EnsureSuccessStatusCode();
        (await user.Client.PostAsync($"{CategoriesEndpoint}/{gifts.Id}/archive", content: null)).EnsureSuccessStatusCode();

        var sameCategory = await user.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{id}", New(checking.Id, dining.Id, amount: 40m));
        var otherArchived = await user.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{id}", New(checking.Id, gifts.Id, amount: 40m));

        Assert.Equal(HttpStatusCode.OK, sameCategory.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, otherArchived.StatusCode);
        Assert.Equal(60m, (await GetAccountAsync(user, checking.Id)).Balance);
    }

    [Fact]
    public async Task Another_Users_Transaction_is_not_found_to_edit_or_delete_and_cant_be_moved_to_their_Account_or_Category()
    {
        var owner = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var ownerAccount = await OpenAccountAsync(owner, openingBalance: 100m);
        var ownerCategory = await CategoryAsync(owner, "Dining");
        var id = await RecordAsync(owner, New(ownerAccount.Id, ownerCategory.Id, amount: 30m));
        var stranger = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var strangerAccount = await OpenAccountAsync(stranger, openingBalance: 100m);
        var strangerCategory = await CategoryAsync(stranger, "Dining");
        var strangerTransaction = await RecordAsync(stranger, New(strangerAccount.Id, strangerCategory.Id, amount: 5m));

        var edit = await stranger.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{id}", New(strangerAccount.Id, strangerCategory.Id));
        var delete = await stranger.Client.DeleteAsync($"{TransactionsEndpoint}/{id}");
        var toOthersAccount = await stranger.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{strangerTransaction}", New(ownerAccount.Id, strangerCategory.Id));
        var toOthersCategory = await stranger.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{strangerTransaction}", New(strangerAccount.Id, ownerCategory.Id));

        Assert.All([edit, delete, toOthersAccount, toOthersCategory], response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
        Assert.Equal(70m, (await GetAccountAsync(owner, ownerAccount.Id)).Balance);
        Assert.Equal(95m, (await GetAccountAsync(stranger, strangerAccount.Id)).Balance);
    }

    [Fact]
    public async Task A_back_dated_Transaction_before_the_Opening_Balance_date_is_accepted_without_changing_the_balance()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);

        var created = await user.Client.PostAsJsonAsync(
            TransactionsEndpoint, New(checking.Id, (await CategoryAsync(user, "Dining")).Id, date: Today.AddDays(-90), amount: 40m));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(100m, (await GetAccountAsync(user, checking.Id)).Balance);
    }

    [Fact]
    public async Task A_Credit_Card_purchase_increases_what_is_owed()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var created = await user.Client.PostAsJsonAsync(AccountsEndpoint, new
        {
            name = "Visa",
            kind = "CreditCard",
            openingBalance = new { amount = -500m, date = Today.AddDays(-30) },
        });
        var visa = (await created.Content.ReadFromJsonAsync<AccountView>())!;

        await RecordAsync(user, New(visa.Id, (await CategoryAsync(user, "Shopping")).Id, amount: 120m));

        Assert.Equal(-620m, (await GetAccountAsync(user, visa.Id)).Balance);
    }

    [Fact]
    public async Task An_Account_with_Transactions_cant_change_its_Currency()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, openingBalance: 100m);
        await RecordAsync(user, New(checking.Id, (await CategoryAsync(user, "Dining")).Id));

        var changed = await user.Client.PutAsJsonAsync($"{AccountsEndpoint}/{checking.Id}/currency", new { currency = "EUR" });

        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
    }

    private static async Task<Guid> RecordAsync(SignedInUser user, object transaction)
    {
        var created = await user.Client.PostAsJsonAsync(TransactionsEndpoint, transaction);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<TransactionView>())!.Id;
    }

    private static async Task<List<Guid>> ListAsync(SignedInUser user, string query) =>
        (await user.Client.GetFromJsonAsync<List<TransactionView>>(TransactionsEndpoint + query))!.Select(t => t.Id).ToList();

    private static object New(
        Guid account, Guid category, DateOnly? date = null, object? direction = null, decimal amount = 1m, string? note = null) =>
        new { accountId = account, categoryId = category, date = date ?? Today, direction = direction ?? "Out", amount, note };

    private static async Task<AccountView> OpenAccountAsync(SignedInUser user, decimal openingBalance, string? currency = null)
    {
        var created = await user.Client.PostAsJsonAsync(AccountsEndpoint, new
        {
            name = "Checking",
            kind = "Checking",
            currency,
            openingBalance = new { amount = openingBalance, date = Today.AddDays(-30) },
        });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<AccountView>())!;
    }

    private static async Task<AccountView> GetAccountAsync(SignedInUser user, Guid id) =>
        (await user.Client.GetFromJsonAsync<AccountView>($"{AccountsEndpoint}/{id}"))!;

    private static async Task<CategoryView> CategoryAsync(SignedInUser user, string name)
    {
        var tree = await user.Client.GetFromJsonAsync<List<CategoryView>>(CategoriesEndpoint);
        return Assert.Single(tree!, node => node.Name == name);
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record TransactionView(Guid Id, Guid AccountId, DateOnly Date, string Direction, decimal Amount, Guid CategoryId, string? Note);

    private sealed record AccountView(Guid Id, decimal Balance);

    private sealed record CategoryView(Guid Id, string Name);
}
