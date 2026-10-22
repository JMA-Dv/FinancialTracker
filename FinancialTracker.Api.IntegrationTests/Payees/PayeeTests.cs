using System.Net;
using System.Net.Http.Json;
using FinancialTracker.Api.IntegrationTests.Infrastructure;

namespace FinancialTracker.Api.IntegrationTests.Payees;

[Collection(ApiCollection.Name)]
public sealed class PayeeTests(ApiFactory api)
{
    private const string PayeesEndpoint = "/api/payees";
    private const string TransactionsEndpoint = "/api/transactions";
    private const string AccountsEndpoint = "/api/accounts";
    private const string CategoriesEndpoint = "/api/categories";

    [Fact]
    public async Task A_new_Payee_name_on_a_Transaction_creates_the_Payee_and_a_known_name_ignoring_case_reuses_it()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (account, dining, _) = await SetUpAsync(user);

        var first = await RecordAsync(user, New(account, dining, payeeName: "Joe's Diner"));
        var second = await RecordAsync(user, New(account, dining, payeeName: "  JOE'S diner "));

        var payee = Assert.Single(await ListPayeesAsync(user));
        Assert.Equal("Joe's Diner", payee.Name);
        Assert.Equal(payee.Id, first.PayeeId);
        Assert.Equal(payee.Id, second.PayeeId);
    }

    [Fact]
    public async Task Editing_a_Transaction_with_an_unknown_Payee_name_creates_the_Payee_and_a_known_name_reuses_it()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (account, dining, _) = await SetUpAsync(user);
        var transaction = await RecordAsync(user, New(account, dining, payeeName: "Joe's Diner"));

        var toNew = await user.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{transaction.Id}", New(account, dining, payeeName: "Bakery"));
        var toKnown = await user.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{transaction.Id}", New(account, dining, payeeName: "joe's diner"));

        Assert.Equal(HttpStatusCode.OK, toNew.StatusCode);
        Assert.Equal(HttpStatusCode.OK, toKnown.StatusCode);
        var payees = await ListPayeesAsync(user);
        Assert.Equal(["Bakery", "Joe's Diner"], payees.Select(p => p.Name));
        Assert.Equal(payees.Single(p => p.Name == "Joe's Diner").Id, (await toKnown.Content.ReadFromJsonAsync<TransactionView>())!.PayeeId);
    }

    [Fact]
    public async Task A_Payee_is_optional_and_editing_without_one_removes_it_from_the_Transaction()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (account, dining, _) = await SetUpAsync(user);
        var withoutPayee = await RecordAsync(user, New(account, dining));
        var withBlankPayee = await RecordAsync(user, New(account, dining, payeeName: "   "));
        var transaction = await RecordAsync(user, New(account, dining, payeeName: "Joe's Diner"));

        var removed = await user.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{transaction.Id}", New(account, dining));

        Assert.Null(withoutPayee.PayeeId);
        Assert.Null(withBlankPayee.PayeeId);
        Assert.Null((await removed.Content.ReadFromJsonAsync<TransactionView>())!.PayeeId);
        Assert.Single(await ListPayeesAsync(user));
    }

    [Fact]
    public async Task A_Payee_name_over_the_limit_is_rejected_and_the_Transaction_is_not_recorded()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (account, dining, _) = await SetUpAsync(user);

        var created = await user.Client.PostAsJsonAsync(TransactionsEndpoint, New(account, dining, payeeName: new string('a', 101)));

        Assert.Equal(HttpStatusCode.BadRequest, created.StatusCode);
        Assert.Empty(await ListTransactionIdsAsync(user, ""));
        Assert.Empty(await ListPayeesAsync(user));
    }

    [Fact]
    public async Task A_Transaction_that_breaks_a_rule_does_not_leave_its_new_Payee_behind()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (account, dining, _) = await SetUpAsync(user);

        var created = await user.Client.PostAsJsonAsync(TransactionsEndpoint, New(account, dining, amount: 0m, payeeName: "Joe's Diner"));

        Assert.Equal(HttpStatusCode.BadRequest, created.StatusCode);
        Assert.Empty(await ListPayeesAsync(user));
    }

    [Fact]
    public async Task Payees_are_listed_by_name_and_can_be_renamed()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (account, dining, _) = await SetUpAsync(user);
        await RecordAsync(user, New(account, dining, payeeName: "Zoo"));
        var bakery = (await RecordAsync(user, New(account, dining, payeeName: "bakery"))).PayeeId!.Value;

        var renamed = await user.Client.PutAsJsonAsync($"{PayeesEndpoint}/{bakery}/name", new { name = "  Corner Bakery " });

        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        Assert.Equal("Corner Bakery", (await renamed.Content.ReadFromJsonAsync<PayeeView>())!.Name);
        Assert.Equal(["Corner Bakery", "Zoo"], (await ListPayeesAsync(user)).Select(p => p.Name));
    }

    [Fact]
    public async Task Renaming_a_Payee_to_another_Payees_name_is_rejected_but_changing_only_its_case_is_fine()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (account, dining, _) = await SetUpAsync(user);
        await RecordAsync(user, New(account, dining, payeeName: "Zoo"));
        var bakery = (await RecordAsync(user, New(account, dining, payeeName: "Bakery"))).PayeeId!.Value;

        var clash = await user.Client.PutAsJsonAsync($"{PayeesEndpoint}/{bakery}/name", new { name = "zoo" });
        var sameNameOtherCase = await user.Client.PutAsJsonAsync($"{PayeesEndpoint}/{bakery}/name", new { name = "BAKERY" });

        Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
        Assert.Equal(HttpStatusCode.OK, sameNameOtherCase.StatusCode);
        Assert.Equal(["BAKERY", "Zoo"], (await ListPayeesAsync(user)).Select(p => p.Name));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Renaming_a_Payee_to_a_blank_or_missing_name_is_rejected(string? name)
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (account, dining, _) = await SetUpAsync(user);
        var bakery = (await RecordAsync(user, New(account, dining, payeeName: "Bakery"))).PayeeId!.Value;

        var renamed = await user.Client.PutAsJsonAsync($"{PayeesEndpoint}/{bakery}/name", new { name });

        Assert.Equal(HttpStatusCode.BadRequest, renamed.StatusCode);
        Assert.Equal(["Bakery"], (await ListPayeesAsync(user)).Select(p => p.Name));
    }

    [Fact]
    public async Task A_Payee_rename_shows_on_every_Transaction_that_uses_it()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (account, dining, _) = await SetUpAsync(user);
        var one = await RecordAsync(user, New(account, dining, payeeName: "Bakery"));
        var two = await RecordAsync(user, New(account, dining, payeeName: "bakery"));

        await user.Client.PutAsJsonAsync($"{PayeesEndpoint}/{one.PayeeId}/name", new { name = "Corner Bakery" });
        var again = await RecordAsync(user, New(account, dining, payeeName: "corner bakery"));

        Assert.Equal(one.PayeeId, two.PayeeId);
        Assert.Equal(one.PayeeId, again.PayeeId);
        Assert.Single(await ListPayeesAsync(user));
    }

    [Fact]
    public async Task Transactions_can_be_filtered_by_Payee_alongside_the_other_filters()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (account, dining, gifts) = await SetUpAsync(user);
        var bakeryOld = await RecordAsync(user, New(account, dining, date: Today.AddDays(-5), payeeName: "Bakery"));
        var bakeryNew = await RecordAsync(user, New(account, gifts, date: Today.AddDays(-1), payeeName: "Bakery"));
        await RecordAsync(user, New(account, dining, payeeName: "Zoo"));
        await RecordAsync(user, New(account, dining));
        var bakery = bakeryOld.PayeeId;

        Assert.Equal([bakeryNew.Id, bakeryOld.Id], await ListTransactionIdsAsync(user, $"?payeeId={bakery}"));
        Assert.Equal([bakeryNew.Id], await ListTransactionIdsAsync(user, $"?payeeId={bakery}&categoryId={gifts.Id}"));
    }

    [Fact]
    public async Task The_suggested_Category_is_the_Category_of_the_most_recent_Transaction_with_the_Payee_and_none_without_history()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (account, dining, gifts) = await SetUpAsync(user);
        var created = await RecordAsync(user, New(account, dining, date: Today.AddDays(-10), payeeName: "Bakery"));
        var bakery = created.PayeeId!.Value;
        var noHistory = await RecordAsync(user, New(account, dining, payeeName: "Zoo"));
        await user.Client.DeleteAsync($"{TransactionsEndpoint}/{noHistory.Id}");

        Assert.Equal(dining.Id, SuggestionFor(await ListPayeesAsync(user), "Bakery"));

        await RecordAsync(user, New(account, gifts, date: Today.AddDays(-2), payeeName: "Bakery"));
        // Recorded later but dated earlier, so it is not the most recent one.
        await RecordAsync(user, New(account, dining, date: Today.AddDays(-30), payeeName: "Bakery"));

        var payees = await ListPayeesAsync(user);
        Assert.Equal(gifts.Id, SuggestionFor(payees, "Bakery"));
        Assert.Null(SuggestionFor(payees, "Zoo"));
        Assert.Equal(bakery, payees.Single(p => p.Name == "Bakery").Id);
    }

    [Fact]
    public async Task The_suggested_Category_follows_a_Transaction_that_is_edited_or_deleted()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (account, dining, gifts) = await SetUpAsync(user);
        await RecordAsync(user, New(account, dining, date: Today.AddDays(-5), payeeName: "Bakery"));
        var latest = await RecordAsync(user, New(account, gifts, date: Today.AddDays(-1), payeeName: "Bakery"));

        await user.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{latest.Id}", New(account, dining, payeeName: "Bakery"));
        Assert.Equal(dining.Id, SuggestionFor(await ListPayeesAsync(user), "Bakery"));

        await user.Client.PutAsJsonAsync($"{TransactionsEndpoint}/{latest.Id}", New(account, gifts, payeeName: "Bakery"));
        Assert.Equal(gifts.Id, SuggestionFor(await ListPayeesAsync(user), "Bakery"));

        await user.Client.DeleteAsync($"{TransactionsEndpoint}/{latest.Id}");
        Assert.Equal(dining.Id, SuggestionFor(await ListPayeesAsync(user), "Bakery"));
    }

    [Fact]
    public async Task Another_Users_Payee_is_never_reused_and_returns_404_to_rename_or_filter_by()
    {
        var owner = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (ownerAccount, ownerDining, _) = await SetUpAsync(owner);
        var ownersPayee = (await RecordAsync(owner, New(ownerAccount, ownerDining, payeeName: "Bakery"))).PayeeId!.Value;
        var stranger = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var (strangerAccount, strangerDining, _) = await SetUpAsync(stranger);

        var theirs = await RecordAsync(stranger, New(strangerAccount, strangerDining, payeeName: "bakery"));
        var rename = await stranger.Client.PutAsJsonAsync($"{PayeesEndpoint}/{ownersPayee}/name", new { name = "Mine now" });
        var filter = await stranger.Client.GetAsync($"{TransactionsEndpoint}?payeeId={ownersPayee}");

        Assert.NotEqual(ownersPayee, theirs.PayeeId);
        Assert.Equal(HttpStatusCode.NotFound, rename.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, filter.StatusCode);
        Assert.Equal(["Bakery"], (await ListPayeesAsync(owner)).Select(p => p.Name));
        Assert.Equal(["bakery"], (await ListPayeesAsync(stranger)).Select(p => p.Name));
    }

    [Fact]
    public async Task Payees_need_a_signed_in_User()
    {
        var anonymous = api.CreateAnonymousClient();

        var list = await anonymous.GetAsync(PayeesEndpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
    }

    private static Guid? SuggestionFor(List<PayeeView> payees, string name) => payees.Single(p => p.Name == name).SuggestedCategoryId;

    private async Task<(Guid Account, CategoryView Dining, CategoryView Gifts)> SetUpAsync(SignedInUser user)
    {
        var created = await user.Client.PostAsJsonAsync(AccountsEndpoint, new
        {
            name = "Checking",
            kind = "Checking",
            openingBalance = new { amount = 1000m, date = Today.AddDays(-60) },
        });
        created.EnsureSuccessStatusCode();
        var account = (await created.Content.ReadFromJsonAsync<AccountView>())!;
        var tree = (await user.Client.GetFromJsonAsync<List<CategoryView>>(CategoriesEndpoint))!;
        return (account.Id, tree.Single(c => c.Name == "Dining"), tree.Single(c => c.Name == "Gifts"));
    }

    private static async Task<TransactionView> RecordAsync(SignedInUser user, object transaction)
    {
        var created = await user.Client.PostAsJsonAsync(TransactionsEndpoint, transaction);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<TransactionView>())!;
    }

    private static async Task<List<PayeeView>> ListPayeesAsync(SignedInUser user) =>
        (await user.Client.GetFromJsonAsync<List<PayeeView>>(PayeesEndpoint))!;

    private static async Task<List<Guid>> ListTransactionIdsAsync(SignedInUser user, string query) =>
        (await user.Client.GetFromJsonAsync<List<TransactionView>>(TransactionsEndpoint + query))!.Select(t => t.Id).ToList();

    private static object New(Guid account, CategoryView category, DateOnly? date = null, decimal amount = 1m, string? payeeName = null) =>
        new { accountId = account, categoryId = category.Id, date = date ?? Today, direction = "Out", amount, payeeName };

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record TransactionView(Guid Id, Guid? PayeeId);

    private sealed record PayeeView(Guid Id, string Name, Guid? SuggestedCategoryId);

    private sealed record AccountView(Guid Id);

    private sealed record CategoryView(Guid Id, string Name);
}
