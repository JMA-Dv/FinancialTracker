using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.Payees;

public sealed class PayeeNotFoundException(Guid payeeId)
    : DomainNotFoundException("Payee not found", $"You have no Payee {payeeId}.");
