using System.Net;
using System.Net.Http.Json;
using FinancialTracker.Api.IntegrationTests.Infrastructure;

namespace FinancialTracker.Api.IntegrationTests.Reports;

[Collection(ApiCollection.Name)]
public sealed class ReportTests(ApiFactory api)
{
    private const string ReportsEndpoint = "/api/reports";

    [Fact]
    public async Task Net_worth_is_the_sum_of_Account_balances_per_Currency_with_Liability_Accounts_negative()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", "USD", 1000m);
        await OpenAccountAsync(user, "Visa", "CreditCard", "USD", -300m);
        await OpenAccountAsync(user, "Pesos", "Savings", "MXN", 5000m);
        await RecordAsync(user, checking.Id, "Dining", Today, "Out", 50m);

        var netWorth = await NetWorthAsync(user, "");

        Assert.Equal(Today, netWorth.Date);
        Assert.Equal([new CurrencyTotal("MXN", 5000m), new CurrencyTotal("USD", 650m)], netWorth.Totals);
    }

    [Fact]
    public async Task Net_worth_can_be_worked_out_for_a_past_date_and_not_for_a_future_one()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", "USD", 1000m);
        await RecordAsync(user, checking.Id, "Dining", Today.AddDays(-5), "Out", 200m);

        var before = await NetWorthAsync(user, $"?date={Today.AddDays(-6):yyyy-MM-dd}");
        var future = await user.Client.GetAsync($"{ReportsEndpoint}/net-worth?date={Today.AddDays(1):yyyy-MM-dd}");

        Assert.Equal([new CurrencyTotal("USD", 1000m)], before.Totals);
        Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);
    }

    [Fact]
    public async Task Income_and_spending_count_only_Transactions_in_the_range_per_Currency()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", "USD", 1000m);
        var savings = await OpenAccountAsync(user, "Savings", "Savings", "USD", 0m);
        var pesos = await OpenAccountAsync(user, "Pesos", "Savings", "MXN", 5000m);
        await RecordAsync(user, checking.Id, "Income", Today.AddDays(-3), "In", 3000m);
        await RecordAsync(user, checking.Id, "Dining", Today.AddDays(-2), "Out", 40m);
        await RecordAsync(user, checking.Id, "Groceries", Today.AddDays(-1), "Out", 60m);
        await RecordAsync(user, pesos.Id, "Dining", Today.AddDays(-1), "Out", 200m);
        await RecordAsync(user, checking.Id, "Dining", Today.AddDays(-20), "Out", 999m);
        await TransferAsync(user, checking.Id, savings.Id, Today.AddDays(-2), 500m);
        await AdjustAsync(user, checking.Id, Today.AddDays(-1), 9000m);

        var report = await IncomeSpendingAsync(user, Today.AddDays(-10), Today);

        Assert.Equal(
            [new IncomeSpendingTotal("MXN", 0m, 200m), new IncomeSpendingTotal("USD", 3000m, 100m)], report.Totals);
    }

    [Fact]
    public async Task A_Refund_reduces_spending_and_leaves_income_unchanged()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", "USD", 1000m);
        await RecordAsync(user, checking.Id, "Income", Today.AddDays(-3), "In", 2000m);
        await RecordAsync(user, checking.Id, "Shopping", Today.AddDays(-2), "Out", 80m);
        await RecordAsync(user, checking.Id, "Shopping", Today.AddDays(-1), "In", 30m);

        var report = await IncomeSpendingAsync(user, Today.AddDays(-10), Today);

        Assert.Equal([new IncomeSpendingTotal("USD", 2000m, 50m)], report.Totals);
    }

    [Fact]
    public async Task Money_taken_out_through_an_Income_Category_reduces_income()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", "USD", 1000m);
        await RecordAsync(user, checking.Id, "Income", Today.AddDays(-3), "In", 2000m);
        await RecordAsync(user, checking.Id, "Income", Today.AddDays(-2), "Out", 300m);

        var report = await IncomeSpendingAsync(user, Today.AddDays(-10), Today);

        Assert.Equal([new IncomeSpendingTotal("USD", 1700m, 0m)], report.Totals);
    }

    [Fact]
    public async Task The_range_includes_both_of_its_end_dates()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", "USD", 1000m);
        await RecordAsync(user, checking.Id, "Dining", Today.AddDays(-11), "Out", 1m);
        await RecordAsync(user, checking.Id, "Dining", Today.AddDays(-10), "Out", 10m);
        await RecordAsync(user, checking.Id, "Dining", Today.AddDays(-5), "Out", 100m);
        await RecordAsync(user, checking.Id, "Dining", Today.AddDays(-4), "Out", 1000m);

        var report = await IncomeSpendingAsync(user, Today.AddDays(-10), Today.AddDays(-5));

        Assert.Equal([new IncomeSpendingTotal("USD", 0m, 110m)], report.Totals);
    }

    [Fact]
    public async Task A_range_that_ends_before_it_starts_or_is_missing_an_end_is_rejected()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");

        var backwards = await user.Client.GetAsync($"{ReportsEndpoint}/income-spending?from={Today:yyyy-MM-dd}&to={Today.AddDays(-1):yyyy-MM-dd}");
        var noFrom = await user.Client.GetAsync($"{ReportsEndpoint}/income-spending?to={Today:yyyy-MM-dd}");
        var noTo = await user.Client.GetAsync($"{ReportsEndpoint}/income-spending?from={Today:yyyy-MM-dd}");

        Assert.All([backwards, noFrom, noTo], response => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode));
    }

    [Fact]
    public async Task Closed_Accounts_and_Archived_Categories_still_count_for_the_period_they_were_in_use()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var old = await OpenAccountAsync(user, "Old", "Checking", "USD", 0m);
        await RecordAsync(user, old.Id, "Income", Today.AddDays(-20), "In", 100m);
        await RecordAsync(user, old.Id, "Dining", Today.AddDays(-15), "Out", 100m);
        (await user.Client.PostAsync($"/api/accounts/{old.Id}/close", content: null)).EnsureSuccessStatusCode();
        var dining = Assert.Single((await user.Client.GetFromJsonAsync<List<CategoryView>>("/api/categories"))!, c => c.Name == "Dining");
        (await user.Client.PostAsync($"/api/categories/{dining.Id}/archive", content: null)).EnsureSuccessStatusCode();

        var report = await IncomeSpendingAsync(user, Today.AddDays(-25), Today);

        Assert.Equal([new IncomeSpendingTotal("USD", 100m, 100m)], report.Totals);
    }

    [Fact]
    public async Task A_parent_Category_total_includes_its_own_Transactions_and_all_its_children()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", "USD", 1000m);
        var pesos = await OpenAccountAsync(user, "Pesos", "Savings", "MXN", 5000m);
        var dining = await CategoryAsync(user, "Dining");
        var restaurants = await CreateCategoryAsync(user, "Restaurants", dining.Id);
        var cafes = await CreateCategoryAsync(user, "Cafes", dining.Id);
        var groceries = await CategoryAsync(user, "Groceries");
        var salary = await CreateCategoryAsync(user, "Salary", (await CategoryAsync(user, "Income")).Id);
        await RecordAsync(user, checking.Id, dining.Id, Today.AddDays(-5), "Out", 10m);
        await RecordAsync(user, checking.Id, restaurants.Id, Today.AddDays(-4), "Out", 45m);
        await RecordAsync(user, checking.Id, restaurants.Id, Today.AddDays(-3), "In", 5m);
        await RecordAsync(user, checking.Id, cafes.Id, Today.AddDays(-2), "Out", 8m);
        await RecordAsync(user, checking.Id, groceries.Id, Today.AddDays(-2), "Out", 70m);
        await RecordAsync(user, checking.Id, salary.Id, Today.AddDays(-1), "In", 2000m);
        await RecordAsync(user, pesos.Id, groceries.Id, Today.AddDays(-1), "Out", 300m);

        var report = await CategoryBreakdownAsync(user, Today.AddDays(-10), Today);

        Assert.Equal(["MXN", "USD"], report.Currencies.Select(c => c.Currency));
        Assert.Equal(
            Shape([new CategoryTotalView(groceries.Id, "Groceries", "Expense", false, 300m, 300m, [])]), Shape(report.Currencies[0].Categories));
        Assert.Equal(
            Shape(
            [
                new CategoryTotalView(salary.ParentId!.Value, "Income", "Income", false, 2000m, 0m,
                    [new CategoryTotalView(salary.Id, "Salary", "Income", false, 2000m, 2000m, [])]),
                new CategoryTotalView(groceries.Id, "Groceries", "Expense", false, 70m, 70m, []),
                new CategoryTotalView(dining.Id, "Dining", "Expense", false, 58m, 10m,
                [
                    new CategoryTotalView(restaurants.Id, "Restaurants", "Expense", false, 40m, 40m, []),
                    new CategoryTotalView(cafes.Id, "Cafes", "Expense", false, 8m, 8m, []),
                ]),
            ]),
            Shape(report.Currencies[1].Categories));
    }

    [Fact]
    public async Task The_Category_breakdown_leaves_out_Categories_without_Transactions_in_the_range_and_keeps_Archived_ones()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", "USD", 1000m);
        var dining = await CategoryAsync(user, "Dining");
        var restaurants = await CreateCategoryAsync(user, "Restaurants", dining.Id);
        await RecordAsync(user, checking.Id, restaurants.Id, Today.AddDays(-5), "Out", 45m);
        await RecordAsync(user, checking.Id, (await CategoryAsync(user, "Gifts")).Id, Today.AddDays(-50), "Out", 99m);
        (await user.Client.PostAsync($"/api/categories/{restaurants.Id}/archive", content: null)).EnsureSuccessStatusCode();
        (await user.Client.PostAsync($"/api/categories/{dining.Id}/archive", content: null)).EnsureSuccessStatusCode();

        var report = await CategoryBreakdownAsync(user, Today.AddDays(-10), Today);

        var currency = Assert.Single(report.Currencies);
        Assert.Equal(
            Shape(
            [
                new CategoryTotalView(dining.Id, "Dining", "Expense", true, 45m, 0m,
                    [new CategoryTotalView(restaurants.Id, "Restaurants", "Expense", true, 45m, 45m, [])]),
            ]),
            Shape(currency.Categories));
    }

    [Fact]
    public async Task The_Transactions_listed_for_a_parent_Category_add_up_to_its_total_in_the_Category_breakdown()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", "USD", 1000m);
        var groceries = await CategoryAsync(user, "Groceries");
        var market = await CreateCategoryAsync(user, "Market", groceries.Id);
        await RecordAsync(user, checking.Id, groceries.Id, Today.AddDays(-5), "Out", 100m);
        await RecordAsync(user, checking.Id, market.Id, Today.AddDays(-4), "Out", 220m);
        await RecordAsync(user, checking.Id, market.Id, Today.AddDays(-3), "In", 20m);
        await RecordAsync(user, checking.Id, market.Id, Today.AddDays(-40), "Out", 999m);
        (await user.Client.PostAsync($"/api/categories/{market.Id}/archive", content: null)).EnsureSuccessStatusCode();
        var (from, to) = (Today.AddDays(-10), Today);

        var report = await CategoryBreakdownAsync(user, from, to);
        var listed = (await user.Client.GetFromJsonAsync<List<TransactionView>>(
            $"/api/transactions?categoryId={groceries.Id}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"))!;

        var total = Assert.Single(Assert.Single(report.Currencies).Categories, c => c.Id == groceries.Id).Total;
        Assert.Equal(300m, total);
        Assert.Equal(total, listed.Sum(t => t.Direction == "Out" ? t.Amount : -t.Amount));
    }

    [Fact]
    public async Task Spending_by_Payee_adds_up_Expense_Transactions_per_Currency_and_nets_off_Refunds()
    {
        var user = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var checking = await OpenAccountAsync(user, "Checking", "Checking", "USD", 1000m);
        var pesos = await OpenAccountAsync(user, "Pesos", "Savings", "MXN", 5000m);
        await RecordAsync(user, checking.Id, "Dining", Today.AddDays(-5), "Out", 30m, payee: "Cafe Luna");
        await RecordAsync(user, checking.Id, "Groceries", Today.AddDays(-4), "Out", 20m, payee: "cafe luna");
        await RecordAsync(user, checking.Id, "Shopping", Today.AddDays(-3), "Out", 10m, payee: "Bookshop");
        await RecordAsync(user, checking.Id, "Shopping", Today.AddDays(-2), "In", 4m, payee: "Bookshop");
        await RecordAsync(user, checking.Id, "Dining", Today.AddDays(-2), "Out", 5m);
        await RecordAsync(user, checking.Id, "Income", Today.AddDays(-1), "In", 2000m, payee: "Employer");
        await RecordAsync(user, checking.Id, "Dining", Today.AddDays(-30), "Out", 999m, payee: "Cafe Luna");
        await RecordAsync(user, pesos.Id, "Dining", Today.AddDays(-1), "Out", 100m, payee: "Cafe Luna");

        var report = await PayeeSpendingAsync(user, Today.AddDays(-10), Today);

        Assert.Equal(["MXN", "USD"], report.Currencies.Select(c => c.Currency));
        Assert.Equal([("Cafe Luna", 100m)], report.Currencies[0].Payees.Select(p => (p.Name!, p.Spending)));
        Assert.Equal(
            [("Cafe Luna", 50m), ("Bookshop", 6m), (null, 5m)],
            report.Currencies[1].Payees.Select(p => (p.Name, p.Spending)));
        Assert.Null(report.Currencies[1].Payees[2].PayeeId);
    }

    [Fact]
    public async Task Reports_only_include_the_signed_in_Users_data()
    {
        var owner = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var ownerChecking = await OpenAccountAsync(owner, "Checking", "Checking", "USD", 1000m);
        await RecordAsync(owner, ownerChecking.Id, "Dining", Today.AddDays(-1), "Out", 40m, payee: "Cafe Luna");
        var stranger = await api.CreateSignedInUserWithCurrencyAsync("USD");
        var strangerChecking = await OpenAccountAsync(stranger, "Checking", "Checking", "USD", 200m);
        await RecordAsync(stranger, strangerChecking.Id, "Dining", Today.AddDays(-1), "Out", 7m, payee: "Bakery");
        var (from, to) = (Today.AddDays(-10), Today);

        var netWorth = await NetWorthAsync(stranger, "");
        var incomeSpending = await IncomeSpendingAsync(stranger, from, to);
        var categories = await CategoryBreakdownAsync(stranger, from, to);
        var payees = await PayeeSpendingAsync(stranger, from, to);

        Assert.Equal([new CurrencyTotal("USD", 193m)], netWorth.Totals);
        Assert.Equal([new IncomeSpendingTotal("USD", 0m, 7m)], incomeSpending.Totals);
        Assert.Equal(7m, Assert.Single(Assert.Single(categories.Currencies).Categories).Total);
        Assert.Equal([("Bakery", 7m)], Assert.Single(payees.Currencies).Payees.Select(p => (p.Name!, p.Spending)));
    }

    [Fact]
    public async Task Reports_need_a_signed_in_User()
    {
        var anonymous = api.CreateAnonymousClient();
        var range = $"from={Today:yyyy-MM-dd}&to={Today:yyyy-MM-dd}";

        var responses = new[]
        {
            await anonymous.GetAsync($"{ReportsEndpoint}/net-worth"),
            await anonymous.GetAsync($"{ReportsEndpoint}/income-spending?{range}"),
            await anonymous.GetAsync($"{ReportsEndpoint}/categories?{range}"),
            await anonymous.GetAsync($"{ReportsEndpoint}/payees?{range}"),
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
    }

    private static async Task<NetWorthView> NetWorthAsync(SignedInUser user, string query) =>
        (await user.Client.GetFromJsonAsync<NetWorthView>($"{ReportsEndpoint}/net-worth{query}"))!;

    private static async Task<IncomeSpendingView> IncomeSpendingAsync(SignedInUser user, DateOnly from, DateOnly to) =>
        (await user.Client.GetFromJsonAsync<IncomeSpendingView>(
            $"{ReportsEndpoint}/income-spending?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"))!;

    private static async Task TransferAsync(SignedInUser user, Guid from, Guid to, DateOnly date, decimal amount) =>
        (await user.Client.PostAsJsonAsync("/api/transfers", new { fromAccountId = from, toAccountId = to, date, amount }))
        .EnsureSuccessStatusCode();

    private static async Task AdjustAsync(SignedInUser user, Guid accountId, DateOnly date, decimal balance) =>
        (await user.Client.PostAsJsonAsync($"/api/accounts/{accountId}/balance-adjustments", new { date, balance }))
        .EnsureSuccessStatusCode();

    private static async Task<PayeeSpendingView> PayeeSpendingAsync(SignedInUser user, DateOnly from, DateOnly to) =>
        (await user.Client.GetFromJsonAsync<PayeeSpendingView>(
            $"{ReportsEndpoint}/payees?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"))!;

    private static async Task<CategoryBreakdownView> CategoryBreakdownAsync(SignedInUser user, DateOnly from, DateOnly to) =>
        (await user.Client.GetFromJsonAsync<CategoryBreakdownView>(
            $"{ReportsEndpoint}/categories?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"))!;

    // Records holding a list compare it by reference, so trees are compared as text.
    private static List<string> Shape(IEnumerable<CategoryTotalView> nodes) =>
        nodes.Select(node =>
            $"{node.Id} {node.Name} {node.Kind} archived={node.IsArchived} total={node.Total:0.####} own={node.OwnAmount:0.####} [{string.Join(", ", Shape(node.Children))}]")
            .ToList();

    private static async Task<CategoryView> CategoryAsync(SignedInUser user, string name) =>
        Assert.Single((await user.Client.GetFromJsonAsync<List<CategoryView>>("/api/categories"))!, node => node.Name == name);

    private static async Task<CategoryView> CreateCategoryAsync(SignedInUser user, string name, Guid parentId)
    {
        var created = await user.Client.PostAsJsonAsync("/api/categories", new { name, parentId });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<CategoryView>())!;
    }

    private static async Task<Guid> RecordAsync(
        SignedInUser user, Guid accountId, string category, DateOnly date, string direction, decimal amount, string? payee = null) =>
        await RecordAsync(user, accountId, (await CategoryAsync(user, category)).Id, date, direction, amount, payee);

    private static async Task<Guid> RecordAsync(
        SignedInUser user, Guid accountId, Guid categoryId, DateOnly date, string direction, decimal amount, string? payee = null)
    {
        var created = await user.Client.PostAsJsonAsync(
            "/api/transactions", new { accountId, date, direction, amount, categoryId, payeeName = payee });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<IdView>())!.Id;
    }

    private static async Task<IdView> OpenAccountAsync(SignedInUser user, string name, string kind, string currency, decimal openingBalance)
    {
        var created = await user.Client.PostAsJsonAsync("/api/accounts", new
        {
            name,
            kind,
            currency,
            openingBalance = new { amount = openingBalance, date = Today.AddDays(-30) },
        });
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<IdView>())!;
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record CurrencyTotal(string Currency, decimal Amount);

    private sealed record NetWorthView(DateOnly Date, List<CurrencyTotal> Totals);

    private sealed record IncomeSpendingTotal(string Currency, decimal Income, decimal Spending);

    private sealed record IncomeSpendingView(DateOnly From, DateOnly To, List<IncomeSpendingTotal> Totals);

    private sealed record PayeeTotalView(Guid? PayeeId, string? Name, decimal Spending);

    private sealed record PayeeCurrencyView(string Currency, List<PayeeTotalView> Payees);

    private sealed record PayeeSpendingView(DateOnly From, DateOnly To, List<PayeeCurrencyView> Currencies);

    private sealed record IdView(Guid Id);

    private sealed record TransactionView(string Direction, decimal Amount);

    private sealed record CategoryTotalView(
        Guid Id, string Name, string Kind, bool IsArchived, decimal Total, decimal OwnAmount, List<CategoryTotalView> Children);

    private sealed record CategoryCurrencyView(string Currency, List<CategoryTotalView> Categories);

    private sealed record CategoryBreakdownView(DateOnly From, DateOnly To, List<CategoryCurrencyView> Currencies);

    private sealed record CategoryView(Guid Id, string Name, Guid? ParentId);
}
