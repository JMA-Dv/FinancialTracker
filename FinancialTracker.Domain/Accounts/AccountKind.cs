namespace FinancialTracker.Domain.Accounts;

/// <summary>What kind of place an Account is. It only affects display and the asset/liability split.</summary>
public enum AccountKind
{
    Checking,
    Savings,
    Cash,

    /// <summary>A Liability Account: its balance is normally money the User owes.</summary>
    CreditCard,
}
