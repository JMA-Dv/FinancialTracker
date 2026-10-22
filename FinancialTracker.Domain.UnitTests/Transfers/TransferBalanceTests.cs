using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Money;
using FinancialTracker.Domain.Transfers;

namespace FinancialTracker.Domain.UnitTests.Transfers;

public sealed class TransferBalanceTests
{
    private static readonly DateOnly Today = new(2026, 10, 18);
    private static readonly Currency Usd = Currency.FromCode("USD");
    private static readonly Currency Eur = Currency.FromCode("EUR");

    [Fact]
    public void A_Transfer_lowers_the_from_Account_raises_the_to_Account_and_leaves_others_alone()
    {
        var userId = Guid.NewGuid();
        var checking = Open(userId, AccountKind.Checking, Usd, new OpeningBalance(500m, new DateOnly(2026, 9, 1)));
        var savings = Open(userId, AccountKind.Savings, Usd, new OpeningBalance(100m, new DateOnly(2026, 9, 1)));
        var wallet = Open(userId, AccountKind.Cash, Usd, openingBalance: null);
        var transfer = Transfer.Create(checking, savings, new DateOnly(2026, 10, 1), 200m, receivedAmount: null, note: null, Today);

        Assert.Equal(300m, checking.BalanceOn(Today, [], [transfer]));
        Assert.Equal(300m, savings.BalanceOn(Today, [], [transfer]));
        Assert.Equal(0m, wallet.BalanceOn(Today, [], [transfer]));
    }

    [Fact]
    public void A_Transfer_between_Currencies_changes_each_balance_by_its_own_amount()
    {
        var userId = Guid.NewGuid();
        var dollars = Open(userId, AccountKind.Checking, Usd, new OpeningBalance(500m, new DateOnly(2026, 9, 1)));
        var euros = Open(userId, AccountKind.Savings, Eur, new OpeningBalance(10m, new DateOnly(2026, 9, 1)));
        var transfer = Transfer.Create(dollars, euros, new DateOnly(2026, 10, 1), 100m, receivedAmount: 92.40m, note: null, Today);

        Assert.Equal(400m, dollars.BalanceOn(Today, [], [transfer]));
        Assert.Equal(102.40m, euros.BalanceOn(Today, [], [transfer]));
    }

    [Fact]
    public void A_Transfer_dated_on_or_before_a_Balance_Adjustment_is_already_inside_it_and_one_after_a_date_is_left_out_of_it()
    {
        var userId = Guid.NewGuid();
        var checking = Open(userId, AccountKind.Checking, Usd, new OpeningBalance(500m, new DateOnly(2026, 9, 1)));
        var savings = Open(userId, AccountKind.Savings, Usd, openingBalance: null);
        var beforeOpening = Transfer.Create(checking, savings, new DateOnly(2026, 9, 1), 50m, null, null, Today);
        var afterOpening = Transfer.Create(checking, savings, new DateOnly(2026, 10, 10), 20m, null, null, Today);

        Assert.Equal(480m, checking.BalanceOn(Today, [], [beforeOpening, afterOpening]));
        Assert.Equal(500m, checking.BalanceOn(new DateOnly(2026, 10, 9), [], [beforeOpening, afterOpening]));
        Assert.Equal(70m, savings.BalanceOn(Today, [], [beforeOpening, afterOpening]));
    }

    [Fact]
    public void Paying_a_Credit_Card_from_Checking_reduces_what_is_owed()
    {
        var userId = Guid.NewGuid();
        var checking = Open(userId, AccountKind.Checking, Usd, new OpeningBalance(500m, new DateOnly(2026, 9, 1)));
        var visa = Open(userId, AccountKind.CreditCard, Usd, new OpeningBalance(-300m, new DateOnly(2026, 9, 1)));
        var payment = Transfer.Create(checking, visa, new DateOnly(2026, 10, 1), 120m, null, null, Today);

        Assert.Equal(-180m, visa.BalanceOn(Today, [], [payment]));
    }

    private static Account Open(Guid userId, AccountKind kind, Currency currency, OpeningBalance? openingBalance) =>
        Account.Open(userId, "Account", kind, currency, openingBalance, Today);
}
