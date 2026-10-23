using FinancialTracker.Application.Payees;
using FinancialTracker.Domain.Categories;
using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Money;

namespace FinancialTracker.Application.Reports;

/// <summary>Adds up the User's spending over a date range by Payee, per Currency.</summary>
public sealed class GetPayeeSpending(ReportEntries entries, IPayeeStore payees)
{
    /// <exception cref="DomainValidationException">The range ends before it starts.</exception>
    public async Task<PayeeSpending> ExecuteAsync(Guid userId, DateRange range)
    {
        var spending = (await entries.LoadAsync(userId, range)).Where(entry => entry.Category.Kind is CategoryKind.Expense).ToList();
        var nameById = (await payees.ListAsync(userId)).ToDictionary(payee => payee.Id, payee => payee.Name);

        var currencies = spending
            .GroupBy(entry => entry.Currency)
            .OrderBy(group => group.Key.Code, StringComparer.Ordinal)
            .Select(group => new PayeeSpendingCurrency(
                group.Key,
                group
                    .GroupBy(entry => entry.Transaction.PayeeId)
                    .Select(payee => new PayeeTotal(
                        payee.Key, payee.Key is { } id ? nameById[id] : null, payee.Sum(entry => entry.Amount)))
                    // Spending on no Payee goes last, so it reads as the remainder of the total.
                    .OrderBy(total => total.PayeeId is null)
                    .ThenByDescending(total => total.Spending)
                    .ThenBy(total => total.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()))
            .ToList();
        return new PayeeSpending(range, currencies);
    }
}

public sealed record PayeeSpending(DateRange Range, IReadOnlyList<PayeeSpendingCurrency> Currencies);

public sealed record PayeeSpendingCurrency(Currency Currency, IReadOnlyList<PayeeTotal> Payees);

/// <param name="PayeeId">Null for spending on Transactions that name no Payee.</param>
/// <param name="Name">The Payee's name, or null when there is no Payee.</param>
/// <param name="Spending">Money spent with the Payee, less any Refunds from them.</param>
public sealed record PayeeTotal(Guid? PayeeId, string? Name, decimal Spending);
