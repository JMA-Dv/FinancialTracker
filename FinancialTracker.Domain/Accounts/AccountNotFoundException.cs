using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.Accounts;

public sealed class AccountNotFoundException(Guid accountId)
    : DomainNotFoundException("Account not found", $"You have no Account {accountId}.");
