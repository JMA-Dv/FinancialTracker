namespace FinancialTracker.Application.Accounts;

/// <param name="Balance">The signed balance at the end of the date: negative is money owed.</param>
public sealed record BalanceAdjustmentDetails(DateOnly Date, decimal Balance);
