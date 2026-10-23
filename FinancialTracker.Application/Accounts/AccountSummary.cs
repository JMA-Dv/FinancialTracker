using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Money;
using FinancialTracker.Domain.Transactions;
using FinancialTracker.Domain.Transfers;

namespace FinancialTracker.Application.Accounts;

/// <summary>An Account as the User sees it, with its current balance.</summary>
public sealed record AccountSummary(
    Guid Id,
    string Name,
    AccountKind Kind,
    Currency Currency,
    decimal Balance,
    bool IsLiability,
    bool IsClosed,
    OpeningBalance? OpeningBalance)
{
    public static AccountSummary Of(
        Account account, IEnumerable<Transaction> transactions, IEnumerable<Transfer> transfers, DateOnly today) =>
        new(
            account.Id,
            account.Name,
            account.Kind,
            account.Currency,
            account.BalanceOn(today, transactions, transfers),
            account.IsLiability,
            account.IsClosed,
            account.OpeningBalance is { } opening ? new OpeningBalance(opening.Balance, opening.Date) : null);
}
