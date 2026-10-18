using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Money;

namespace FinancialTracker.Domain.UnitTests.Accounts;

public sealed class AccountTests
{
    private static readonly DateOnly Today = new(2026, 10, 18);
    private static readonly Currency Usd = Currency.FromCode("USD");
    private static readonly Currency Jpy = Currency.FromCode("JPY");

    [Fact]
    public void An_Account_without_an_Opening_Balance_is_at_zero_on_every_date()
    {
        var account = Open(openingBalance: null);

        Assert.Equal(0m, account.BalanceOn(new DateOnly(1990, 1, 1)));
        Assert.Equal(0m, account.BalanceOn(Today));
    }

    [Fact]
    public void An_Account_holds_its_Opening_Balance_from_the_date_tracking_starts()
    {
        var account = Open(new OpeningBalance(250.75m, new DateOnly(2026, 9, 1)));

        Assert.Equal(0m, account.BalanceOn(new DateOnly(2026, 8, 31)));
        Assert.Equal(250.75m, account.BalanceOn(new DateOnly(2026, 9, 1)));
        Assert.Equal(250.75m, account.BalanceOn(Today));
    }

    [Fact]
    public void An_Account_with_Transactions_or_Transfers_cant_change_its_Currency()
    {
        var account = Open(openingBalance: null);

        Assert.Throws<DomainConflictException>(() => account.ChangeCurrency(Jpy, hasTransactionsOrTransfers: true));
        Assert.Equal(Usd, account.Currency);
    }

    [Fact]
    public void An_Account_with_Transactions_or_Transfers_can_be_set_to_the_Currency_it_already_has()
    {
        var account = Open(openingBalance: null);

        account.ChangeCurrency(Usd, hasTransactionsOrTransfers: true);

        Assert.Equal(Usd, account.Currency);
    }

    [Fact]
    public void An_Account_without_Transactions_or_Transfers_can_change_its_Currency()
    {
        var account = Open(new OpeningBalance(1200m, Today));

        account.ChangeCurrency(Jpy, hasTransactionsOrTransfers: false);

        Assert.Equal(Jpy, account.Currency);
        Assert.Equal(1200m, account.BalanceOn(Today));
    }

    [Fact]
    public void An_Opening_Balance_that_doesnt_fit_the_new_Currency_blocks_the_change()
    {
        var account = Open(new OpeningBalance(10.50m, Today));

        Assert.Throws<DomainValidationException>(() => account.ChangeCurrency(Jpy, hasTransactionsOrTransfers: false));
        Assert.Equal(Usd, account.Currency);
    }

    private static Account Open(OpeningBalance? openingBalance) =>
        Account.Open(Guid.NewGuid(), "Checking", AccountKind.Checking, Usd, openingBalance, Today);
}
