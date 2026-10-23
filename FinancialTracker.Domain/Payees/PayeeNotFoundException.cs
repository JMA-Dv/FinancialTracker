using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.Payees;

/// <summary>The Payee doesn't exist, or belongs to another User.</summary>
public sealed class PayeeNotFoundException(Guid payeeId)
    : DomainNotFoundException("Payee not found", $"You have no Payee {payeeId}.");
