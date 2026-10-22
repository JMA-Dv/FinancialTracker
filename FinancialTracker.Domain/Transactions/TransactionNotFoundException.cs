using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.Transactions;

public sealed class TransactionNotFoundException(Guid transactionId)
    : DomainNotFoundException("Transaction not found", $"You have no Transaction {transactionId}.");
