namespace FinancialTracker.Domain.Accounts;

/// <summary>
/// A statement that an Account's real balance was <see cref="Balance"/> at the end of <see cref="Date"/>.
/// It stores the stated balance, never a difference, and is never income or spending.
/// </summary>
public sealed class BalanceAdjustment
{
    internal BalanceAdjustment(Guid id, DateOnly date, decimal balance)
    {
        Id = id;
        Date = date;
        Balance = balance;
    }

    public Guid Id { get; private set; }

    public DateOnly Date { get; private set; }

    /// <summary>The Account's balance at the end of the date, in the Account's Currency. Signed.</summary>
    public decimal Balance { get; private set; }
}
