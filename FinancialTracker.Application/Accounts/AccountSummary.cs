using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Money;

namespace FinancialTracker.Application.Accounts;

/// <summary>An Account as the User sees it, with its current balance.</summary>
public sealed record AccountSummary(
    Guid Id,
    string Name,
    AccountKind Kind,
    Currency Currency,
    decimal Balance,
    bool IsLiability,
    OpeningBalance? OpeningBalance)
{
    public static AccountSummary Of(Account account, DateOnly today) =>
        new(
            account.Id,
            account.Name,
            account.Kind,
            account.Currency,
            account.BalanceOn(today),
            account.IsLiability,
            account.OpeningBalance is { } opening ? new OpeningBalance(opening.Balance, opening.Date) : null);
}
