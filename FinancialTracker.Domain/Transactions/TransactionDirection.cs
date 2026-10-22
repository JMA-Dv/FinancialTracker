namespace FinancialTracker.Domain.Transactions;

/// <summary>Whether a Transaction brings money into its Account or takes it out.</summary>
public enum TransactionDirection
{
    In,
    Out,
}
