using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Categories;
using FinancialTracker.Domain.Money;
using FinancialTracker.Domain.Transactions;

namespace FinancialTracker.Domain.UnitTests.Transactions;

public sealed class TransactionBalanceTests
{
    private static readonly DateOnly Today = new(2026, 10, 18);
    private static readonly Currency Usd = Currency.FromCode("USD");

    [Fact]
    public void Transactions_dated_on_or_before_the_Opening_Balance_are_already_inside_it()
    {
        var account = Open(AccountKind.Checking, new OpeningBalance(100m, new DateOnly(2026, 9, 1)));
        var transactions = new[]
        {
            Record(account, new DateOnly(2026, 8, 15), TransactionDirection.Out, 40m),
            Record(account, new DateOnly(2026, 9, 1), TransactionDirection.Out, 10m),
            Record(account, new DateOnly(2026, 9, 2), TransactionDirection.In, 5m),
        };

        Assert.Equal(105m, account.BalanceOn(Today, transactions, []));
    }

    [Fact]
    public void Without_a_Balance_Adjustment_every_Transaction_counts_whatever_its_date()
    {
        var account = Open(AccountKind.Checking, openingBalance: null);
        var transactions = new[]
        {
            Record(account, new DateOnly(2020, 1, 1), TransactionDirection.In, 50m),
            Record(account, new DateOnly(2026, 10, 1), TransactionDirection.Out, 20m),
        };

        Assert.Equal(30m, account.BalanceOn(Today, transactions, []));
    }

    [Fact]
    public void A_balance_on_a_date_leaves_out_Transactions_dated_after_it()
    {
        var account = Open(AccountKind.Checking, openingBalance: null);
        var transactions = new[]
        {
            Record(account, new DateOnly(2026, 10, 1), TransactionDirection.In, 50m),
            Record(account, new DateOnly(2026, 10, 10), TransactionDirection.Out, 20m),
        };

        Assert.Equal(50m, account.BalanceOn(new DateOnly(2026, 10, 9), transactions, []));
    }

    [Fact]
    public void A_Credit_Card_purchase_increases_what_is_owed()
    {
        var card = Open(AccountKind.CreditCard, new OpeningBalance(-500m, new DateOnly(2026, 9, 1)));
        var purchase = Record(card, new DateOnly(2026, 10, 1), TransactionDirection.Out, 120m);

        Assert.Equal(-620m, card.BalanceOn(Today, [purchase], []));
    }

    private static Account Open(AccountKind kind, OpeningBalance? openingBalance) =>
        Account.Open(Guid.NewGuid(), "Account", kind, Usd, openingBalance, Today);

    private static Transaction Record(Account account, DateOnly date, TransactionDirection direction, decimal amount) =>
        Transaction.Create(
            account, date, direction, amount, Category.Create(account.UserId, "Dining", CategoryKind.Expense, parent: null), payee: null, note: null, Today);
}
