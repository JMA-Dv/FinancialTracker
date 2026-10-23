using FinancialTracker.Domain.Categories;
using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Money;

namespace FinancialTracker.Application.Reports;

/// <summary>Adds up the User's income and spending over a date range, per Currency.</summary>
public sealed class GetIncomeAndSpending(ReportEntries entries)
{
    /// <exception cref="DomainValidationException">The range ends before it starts.</exception>
    public async Task<IncomeAndSpending> ExecuteAsync(Guid userId, DateRange range)
    {
        var totals = (await entries.LoadAsync(userId, range))
            .GroupBy(entry => entry.Currency)
            .Select(group => new IncomeSpendingTotal(
                group.Key,
                group.Where(entry => entry.Category.Kind is CategoryKind.Income).Sum(entry => entry.Amount),
                group.Where(entry => entry.Category.Kind is CategoryKind.Expense).Sum(entry => entry.Amount)))
            .OrderBy(total => total.Currency.Code, StringComparer.Ordinal)
            .ToList();
        return new IncomeAndSpending(range, totals);
    }
}

public sealed record IncomeAndSpending(DateRange Range, IReadOnlyList<IncomeSpendingTotal> Totals);

public sealed record IncomeSpendingTotal(Currency Currency, decimal Income, decimal Spending);
