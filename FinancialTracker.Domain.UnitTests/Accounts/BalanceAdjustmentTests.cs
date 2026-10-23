using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Categories;
using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Money;
using FinancialTracker.Domain.Transactions;
using FinancialTracker.Domain.Transfers;

namespace FinancialTracker.Domain.UnitTests.Accounts;

public sealed class BalanceAdjustmentTests
{
    private static readonly DateOnly Today = new(2026, 10, 18);
    private static readonly Currency Usd = Currency.FromCode("USD");

    [Fact]
    public void A_Balance_Adjustment_sets_the_balance_from_its_date_on()
    {
        var account = Open(openingBalance: null);

        account.AdjustBalance(new DateOnly(2026, 10, 1), 400m, Today);

        Assert.Equal(0m, account.BalanceOn(new DateOnly(2026, 9, 30), [], []));
        Assert.Equal(400m, account.BalanceOn(new DateOnly(2026, 10, 1), [], []));
        Assert.Equal(400m, account.BalanceOn(Today, [], []));
    }

    [Fact]
    public void A_Balance_Adjustment_added_to_an_Account_without_one_becomes_its_Opening_Balance()
    {
        var account = Open(openingBalance: null);

        account.AdjustBalance(new DateOnly(2026, 9, 1), 250m, Today);

        Assert.Equal(new OpeningBalance(250m, new DateOnly(2026, 9, 1)), ToOpeningBalance(account.OpeningBalance));
    }

    [Fact]
    public void A_Balance_Adjustment_can_be_dated_today_but_not_after()
    {
        var account = Open(openingBalance: null);

        account.AdjustBalance(Today, 10m, Today);

        Assert.Throws<DomainValidationException>(() => account.AdjustBalance(Today.AddDays(1), 10m, Today));
        Assert.Single(account.BalanceAdjustments);
    }

    [Fact]
    public void A_Balance_Adjustment_cant_be_more_precise_than_the_Currency()
    {
        var account = Open(openingBalance: null);

        Assert.Throws<DomainValidationException>(() => account.AdjustBalance(Today, 10.005m, Today));
        Assert.Empty(account.BalanceAdjustments);
    }

    [Fact]
    public void Two_Balance_Adjustments_cant_state_the_same_date()
    {
        var account = Open(new OpeningBalance(100m, new DateOnly(2026, 9, 1)));

        Assert.Throws<DomainConflictException>(() => account.AdjustBalance(new DateOnly(2026, 9, 1), 120m, Today));
        Assert.Single(account.BalanceAdjustments);
    }

    [Fact]
    public void A_Balance_Adjustment_can_be_edited_to_another_date_and_balance()
    {
        var account = Open(new OpeningBalance(100m, new DateOnly(2026, 9, 1)));
        var adjustment = account.AdjustBalance(new DateOnly(2026, 10, 1), 400m, Today);

        account.EditBalanceAdjustment(adjustment.Id, new DateOnly(2026, 10, 5), 450m, Today);

        Assert.Equal(100m, account.BalanceOn(new DateOnly(2026, 10, 4), [], []));
        Assert.Equal(450m, account.BalanceOn(new DateOnly(2026, 10, 5), [], []));
    }

    [Fact]
    public void A_Balance_Adjustment_can_be_edited_without_changing_its_date()
    {
        var account = Open(new OpeningBalance(100m, new DateOnly(2026, 9, 1)));
        var adjustment = account.AdjustBalance(new DateOnly(2026, 10, 1), 400m, Today);

        account.EditBalanceAdjustment(adjustment.Id, new DateOnly(2026, 10, 1), 410m, Today);

        Assert.Equal(410m, account.BalanceOn(Today, [], []));
    }

    [Fact]
    public void A_Balance_Adjustment_cant_be_edited_onto_the_date_of_another()
    {
        var account = Open(new OpeningBalance(100m, new DateOnly(2026, 9, 1)));
        var adjustment = account.AdjustBalance(new DateOnly(2026, 10, 1), 400m, Today);

        Assert.Throws<DomainConflictException>(
            () => account.EditBalanceAdjustment(adjustment.Id, new DateOnly(2026, 9, 1), 400m, Today));
        Assert.Equal(new DateOnly(2026, 10, 1), adjustment.Date);
    }

    [Fact]
    public void An_edit_that_breaks_a_rule_changes_nothing()
    {
        var account = Open(new OpeningBalance(100m, new DateOnly(2026, 9, 1)));
        var adjustment = account.AdjustBalance(new DateOnly(2026, 10, 1), 400m, Today);

        Assert.Throws<DomainValidationException>(
            () => account.EditBalanceAdjustment(adjustment.Id, Today.AddDays(1), 999m, Today));
        Assert.Equal(400m, adjustment.Balance);
        Assert.Equal(new DateOnly(2026, 10, 1), adjustment.Date);
    }

    [Fact]
    public void Editing_a_Balance_Adjustment_the_Account_doesnt_have_is_not_found()
    {
        var account = Open(openingBalance: null);

        Assert.Throws<BalanceAdjustmentNotFoundException>(
            () => account.EditBalanceAdjustment(Guid.NewGuid(), Today, 10m, Today));
    }

    [Fact]
    public void Removing_the_only_Balance_Adjustment_starts_the_Account_at_zero_again()
    {
        var account = Open(new OpeningBalance(100m, new DateOnly(2026, 9, 1)));
        var transaction = Record(account, new DateOnly(2026, 10, 1), TransactionDirection.Out, 30m);

        account.RemoveBalanceAdjustment(account.OpeningBalance!.Id);

        Assert.Null(account.OpeningBalance);
        Assert.Equal(-30m, account.BalanceOn(Today, [transaction], []));
    }

    [Fact]
    public void Removing_the_Opening_Balance_makes_the_next_Balance_Adjustment_the_earliest()
    {
        var account = Open(new OpeningBalance(100m, new DateOnly(2026, 9, 1)));
        account.AdjustBalance(new DateOnly(2026, 10, 1), 400m, Today);

        account.RemoveBalanceAdjustment(account.OpeningBalance!.Id);

        Assert.Equal(new OpeningBalance(400m, new DateOnly(2026, 10, 1)), ToOpeningBalance(account.OpeningBalance));
    }

    [Fact]
    public void Removing_a_Balance_Adjustment_the_Account_doesnt_have_is_not_found()
    {
        var account = Open(openingBalance: null);

        Assert.Throws<BalanceAdjustmentNotFoundException>(() => account.RemoveBalanceAdjustment(Guid.NewGuid()));
    }

    [Fact]
    public void Transactions_dated_on_the_day_of_a_Balance_Adjustment_are_considered_included_in_it()
    {
        var account = Open(openingBalance: null);
        account.AdjustBalance(new DateOnly(2026, 10, 1), 400m, Today);
        var sameDay = Record(account, new DateOnly(2026, 10, 1), TransactionDirection.Out, 25m);
        var nextDay = Record(account, new DateOnly(2026, 10, 2), TransactionDirection.Out, 10m);

        Assert.Equal(400m, account.BalanceOn(new DateOnly(2026, 10, 1), [sameDay, nextDay], []));
        Assert.Equal(390m, account.BalanceOn(Today, [sameDay, nextDay], []));
    }

    [Fact]
    public void A_back_dated_Transaction_before_a_later_Balance_Adjustment_leaves_the_adjusted_balance_unchanged()
    {
        var account = Open(new OpeningBalance(100m, new DateOnly(2026, 9, 1)));
        account.AdjustBalance(new DateOnly(2026, 10, 1), 400m, Today);
        var backDated = Record(account, new DateOnly(2026, 9, 15), TransactionDirection.Out, 60m);

        Assert.Equal(400m, account.BalanceOn(Today, [backDated], []));
        Assert.Equal(40m, account.BalanceOn(new DateOnly(2026, 9, 20), [backDated], []));
    }

    [Fact]
    public void A_Balance_Adjustment_shows_the_difference_between_its_stated_balance_and_what_the_tracker_would_show()
    {
        var account = Open(new OpeningBalance(100m, new DateOnly(2026, 9, 1)));
        var spent = Record(account, new DateOnly(2026, 9, 10), TransactionDirection.Out, 30m);
        var adjustment = account.AdjustBalance(new DateOnly(2026, 10, 1), 90m, Today);

        Assert.Equal(20m, account.DifferenceOf(adjustment, [spent], []));
    }

    [Fact]
    public void A_Balance_Adjustment_measures_its_difference_from_the_previous_one_not_from_the_start()
    {
        var account = Open(new OpeningBalance(100m, new DateOnly(2026, 9, 1)));
        account.AdjustBalance(new DateOnly(2026, 10, 1), 90m, Today);
        var spent = Record(account, new DateOnly(2026, 10, 5), TransactionDirection.Out, 10m);
        var second = account.AdjustBalance(new DateOnly(2026, 10, 10), 70m, Today);

        Assert.Equal(-10m, account.DifferenceOf(second, [spent], []));
    }

    [Fact]
    public void A_Balance_Adjustment_counts_Transfers_in_its_difference()
    {
        var account = Open(new OpeningBalance(100m, new DateOnly(2026, 9, 1)));
        var savings = Account.Open(account.UserId, "Savings", AccountKind.Savings, Usd, openingBalance: null, Today);
        var transfer = Transfer.Create(account, savings, new DateOnly(2026, 9, 10), 40m, receivedAmount: null, note: null, Today);
        var adjustment = account.AdjustBalance(new DateOnly(2026, 10, 1), 60m, Today);

        Assert.Equal(0m, account.DifferenceOf(adjustment, [], [transfer]));
    }

    [Fact]
    public void The_Opening_Balance_shows_its_difference_from_the_money_already_recorded()
    {
        var account = Open(openingBalance: null);
        var salary = Record(account, new DateOnly(2026, 9, 1), TransactionDirection.In, 50m);
        var opening = account.AdjustBalance(new DateOnly(2026, 9, 5), 300m, Today);

        Assert.Equal(250m, account.DifferenceOf(opening, [salary], []));
    }

    [Fact]
    public void Another_Accounts_money_is_left_out_of_a_difference()
    {
        var account = Open(new OpeningBalance(100m, new DateOnly(2026, 9, 1)));
        var other = Open(openingBalance: null);
        var elsewhere = Record(other, new DateOnly(2026, 9, 10), TransactionDirection.Out, 30m);
        var adjustment = account.AdjustBalance(new DateOnly(2026, 10, 1), 100m, Today);

        Assert.Equal(0m, account.DifferenceOf(adjustment, [elsewhere], []));
    }

    private static OpeningBalance? ToOpeningBalance(BalanceAdjustment? adjustment) =>
        adjustment is null ? null : new OpeningBalance(adjustment.Balance, adjustment.Date);

    private static Account Open(OpeningBalance? openingBalance) =>
        Account.Open(Guid.NewGuid(), "Checking", AccountKind.Checking, Usd, openingBalance, Today);

    private static Transaction Record(Account account, DateOnly date, TransactionDirection direction, decimal amount) =>
        Transaction.Create(
            account, date, direction, amount, Category.Create(account.UserId, "Dining", CategoryKind.Expense, parent: null), payee: null, note: null, Today);
}
