using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Categories;
using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Money;
using FinancialTracker.Domain.Transactions;
using FinancialTracker.Domain.Transfers;

namespace FinancialTracker.Domain.UnitTests.Accounts;

public sealed class ClosedAccountTests
{
    private static readonly DateOnly Today = new(2026, 10, 18);
    private static readonly Currency Usd = Currency.FromCode("USD");

    [Fact]
    public void A_new_Account_is_open()
    {
        Assert.False(Open(100m).IsClosed);
    }

    [Fact]
    public void An_Account_with_a_zero_balance_can_be_closed()
    {
        var account = Open(0m);

        account.Close(currentBalance: 0m);

        Assert.True(account.IsClosed);
    }

    [Fact]
    public void An_Account_with_a_balance_owed_or_held_cant_be_closed()
    {
        var held = Open(100m);
        var owed = Open(-100m);

        Assert.Throws<DomainConflictException>(() => held.Close(currentBalance: 100m));
        Assert.Throws<DomainConflictException>(() => owed.Close(currentBalance: -100m));
        Assert.False(held.IsClosed);
        Assert.False(owed.IsClosed);
    }

    [Fact]
    public void A_Closed_Account_can_be_reopened()
    {
        var account = Open(0m);
        account.Close(currentBalance: 0m);

        account.Reopen();

        Assert.False(account.IsClosed);
        account.AdjustBalance(Today, 50m, Today);
    }

    [Fact]
    public void A_Closed_Account_rejects_new_edited_and_removed_Balance_Adjustments()
    {
        var account = Open(0m);
        var adjustment = account.OpeningBalance!;
        account.Close(currentBalance: 0m);

        Assert.Throws<DomainConflictException>(() => account.AdjustBalance(Today, 10m, Today));
        Assert.Throws<DomainConflictException>(() => account.EditBalanceAdjustment(adjustment.Id, Today, 10m, Today));
        Assert.Throws<DomainConflictException>(() => account.RemoveBalanceAdjustment(adjustment.Id));
        Assert.Equal(0m, adjustment.Balance);
        Assert.Single(account.BalanceAdjustments);
    }

    [Fact]
    public void A_Balance_Adjustment_the_Closed_Account_doesnt_have_is_still_not_found()
    {
        var account = Open(0m);
        account.Close(currentBalance: 0m);

        Assert.Throws<BalanceAdjustmentNotFoundException>(() => account.EditBalanceAdjustment(Guid.NewGuid(), Today, 1m, Today));
        Assert.Throws<BalanceAdjustmentNotFoundException>(() => account.RemoveBalanceAdjustment(Guid.NewGuid()));
    }

    [Fact]
    public void A_Closed_Accounts_balance_on_past_dates_stays_readable()
    {
        var account = Open(100m);
        var spent = Record(account, new DateOnly(2026, 9, 10), TransactionDirection.Out, 100m);
        account.Close(currentBalance: 0m);

        Assert.Equal(100m, account.BalanceOn(new DateOnly(2026, 9, 9), [spent], []));
        Assert.Equal(0m, account.BalanceOn(Today, [spent], []));
    }

    [Fact]
    public void A_Closed_Account_rejects_a_new_Transaction()
    {
        var account = Open(0m);
        account.Close(currentBalance: 0m);

        Assert.Throws<DomainConflictException>(() => Record(account, Today, TransactionDirection.In, 5m));
    }

    [Fact]
    public void A_Transaction_cant_be_edited_on_or_moved_onto_a_Closed_Account()
    {
        var userId = Guid.NewGuid();
        var open = Open(userId, 0m);
        var closed = Open(userId, 5m);
        var onOpen = Record(open, Today, TransactionDirection.In, 5m);
        var onClosed = Record(closed, Today, TransactionDirection.Out, 5m);
        closed.Close(currentBalance: 0m);

        Assert.Throws<DomainConflictException>(() => Edit(onOpen, open, closed));
        Assert.Throws<DomainConflictException>(() => Edit(onClosed, closed, open));
        Assert.Throws<DomainConflictException>(() => Edit(onClosed, closed, closed));
        Assert.Equal(open.Id, onOpen.AccountId);
        Assert.Equal(closed.Id, onClosed.AccountId);
    }

    [Fact]
    public void A_Transfer_cant_be_created_into_or_out_of_a_Closed_Account()
    {
        var userId = Guid.NewGuid();
        var open = Open(userId, 100m);
        var closed = Open(userId, 0m);
        closed.Close(currentBalance: 0m);

        Assert.Throws<DomainConflictException>(() => Transfer.Create(open, closed, Today, 10m, null, null, Today));
        Assert.Throws<DomainConflictException>(() => Transfer.Create(closed, open, Today, 10m, null, null, Today));
    }

    [Fact]
    public void A_Transfer_cant_be_edited_on_or_moved_onto_a_Closed_Account()
    {
        var userId = Guid.NewGuid();
        var checking = Open(userId, 100m);
        var savings = Open(userId, 0m);
        var wallet = Open(userId, 0m);
        var transfer = Transfer.Create(checking, savings, Today, 10m, null, null, Today);
        wallet.Close(currentBalance: 0m);

        Assert.Throws<DomainConflictException>(
            () => transfer.Edit(checking, savings, checking, wallet, Today, 10m, null, null, Today));
        Assert.Equal(savings.Id, transfer.ToAccountId);
    }

    [Fact]
    public void A_Transfer_on_an_Account_that_has_since_closed_cant_be_edited_away_from_it()
    {
        var userId = Guid.NewGuid();
        var checking = Open(userId, 100m);
        var savings = Open(userId, 0m);
        var wallet = Open(userId, 0m);
        var transfer = Transfer.Create(checking, savings, Today, 10m, null, null, Today);
        Record(savings, Today, TransactionDirection.Out, 10m);
        savings.Close(currentBalance: 0m);

        Assert.Throws<DomainConflictException>(
            () => transfer.Edit(checking, savings, checking, wallet, Today, 10m, null, null, Today));
    }

    [Fact]
    public void Money_can_be_recorded_again_once_the_Account_is_reopened()
    {
        var account = Open(0m);
        account.Close(currentBalance: 0m);
        account.Reopen();

        var transaction = Record(account, Today, TransactionDirection.In, 5m);

        Assert.Equal(5m, account.BalanceOn(Today, [transaction], []));
    }

    private static Account Open(decimal openingBalance) => Open(Guid.NewGuid(), openingBalance);

    private static Account Open(Guid userId, decimal openingBalance) =>
        Account.Open(userId, "Account", AccountKind.Checking, Usd, new OpeningBalance(openingBalance, new DateOnly(2026, 9, 1)), Today);

    private static Transaction Record(Account account, DateOnly date, TransactionDirection direction, decimal amount) =>
        Transaction.Create(
            account, date, direction, amount, Category.Create(account.UserId, "Dining", CategoryKind.Expense, parent: null), payee: null, note: null, Today);

    private static void Edit(Transaction transaction, Account current, Account target) =>
        transaction.Edit(
            current, target, Today, TransactionDirection.In, 1m, Category.Create(current.UserId, "Dining", CategoryKind.Expense, parent: null), payee: null, note: null, Today);
}
