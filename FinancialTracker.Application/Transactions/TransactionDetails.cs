using FinancialTracker.Domain.Transactions;

namespace FinancialTracker.Application.Transactions;

/// <param name="Amount">Positive, in the Account's Currency. The direction says which way it moves.</param>
public sealed record TransactionDetails(
    Guid AccountId, DateOnly Date, TransactionDirection Direction, decimal Amount, Guid CategoryId, string? Note);
