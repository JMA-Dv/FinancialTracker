using FinancialTracker.Domain.Transactions;

namespace FinancialTracker.Application.Transactions;

/// <param name="Amount">Positive, in the Account's Currency. The direction says which way it moves.</param>
/// <param name="PayeeName">Who was paid or paid the User. A name the User has no Payee for yet makes one. Blank means none.</param>
public sealed record TransactionDetails(
    Guid AccountId, DateOnly Date, TransactionDirection Direction, decimal Amount, Guid CategoryId, string? PayeeName, string? Note);
