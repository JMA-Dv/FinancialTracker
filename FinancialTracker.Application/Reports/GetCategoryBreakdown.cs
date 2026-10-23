using FinancialTracker.Domain.Categories;
using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Money;

namespace FinancialTracker.Application.Reports;

/// <summary>Breaks the User's income and spending over a date range down by Category, per Currency.</summary>
public sealed class GetCategoryBreakdown(ReportEntries entries)
{
    /// <exception cref="DomainValidationException">The range ends before it starts.</exception>
    public async Task<CategoryBreakdown> ExecuteAsync(Guid userId, DateRange range)
    {
        var currencies = (await entries.LoadAsync(userId, range))
            .GroupBy(entry => entry.Currency)
            .OrderBy(group => group.Key.Code, StringComparer.Ordinal)
            .Select(group => new CategoryBreakdownCurrency(group.Key, TopLevelTotals(group)))
            .ToList();
        return new CategoryBreakdown(range, currencies);
    }

    // A parent shows up whenever one of its children has money, even if it has none of its own.
    private static IReadOnlyList<CategoryTotal> TopLevelTotals(IEnumerable<ReportEntry> entriesInCurrency) =>
        entriesInCurrency
            .GroupBy(entry => entry.Parent?.Id ?? entry.Category.Id)
            .Select(family =>
            {
                var top = family.First() is { Parent: { } parent } ? parent : family.First().Category;
                var children = family
                    .Where(entry => entry.Category.Id != top.Id)
                    .GroupBy(entry => entry.Category.Id)
                    .Select(child =>
                    {
                        var amount = child.Sum(entry => entry.Amount);
                        return new CategoryTotal(child.First().Category, amount, amount, []);
                    })
                    .OrderByDescending(child => child.Total)
                    .ThenBy(child => child.Category.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
                return new CategoryTotal(
                    top,
                    family.Sum(entry => entry.Amount),
                    family.Where(entry => entry.Category.Id == top.Id).Sum(entry => entry.Amount),
                    children);
            })
            .OrderBy(total => total.Category.Kind)
            .ThenByDescending(total => total.Total)
            .ThenBy(total => total.Category.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
}

public sealed record CategoryBreakdown(DateRange Range, IReadOnlyList<CategoryBreakdownCurrency> Currencies);

public sealed record CategoryBreakdownCurrency(Currency Currency, IReadOnlyList<CategoryTotal> Categories);

/// <param name="Total">Earned in an Income Category, spent in an Expense Category. For a parent, its own plus all its children's.</param>
/// <param name="OwnAmount">What was recorded directly on this Category, leaving its children out.</param>
/// <param name="Children">The Categories under this one that have money in the range. Empty for a child.</param>
public sealed record CategoryTotal(Category Category, decimal Total, decimal OwnAmount, IReadOnlyList<CategoryTotal> Children);
