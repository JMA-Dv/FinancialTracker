using FinancialTracker.Application.Accounts;
using FinancialTracker.Application.Categories;
using FinancialTracker.Application.Transactions;
using FinancialTracker.Domain.Categories;
using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Money;
using FinancialTracker.Domain.Transactions;

namespace FinancialTracker.Application.Reports;

/// <summary>
/// Finds the real earning and spending in a date range: the User's Transactions. Transfers, Balance Adjustments and
/// Opening Balances are not Transactions, so they never get here.
/// </summary>
public sealed class ReportEntries(IAccountStore accounts, ICategoryStore categories, ITransactionStore transactions)
{
    /// <exception cref="DomainValidationException">The range ends before it starts.</exception>
    public async Task<IReadOnlyList<ReportEntry>> LoadAsync(Guid userId, DateRange range)
    {
        if (range.To < range.From)
        {
            throw new DomainValidationException(
                "Invalid date range", $"{range.To:yyyy-MM-dd} is before {range.From:yyyy-MM-dd}.");
        }

        // Closed Accounts and Archived Categories are included: their Transactions happened in the range all the same.
        var currencyByAccount = (await accounts.ListAsync(userId)).ToDictionary(account => account.Id, account => account.Currency);
        var categoryById = (await categories.ListAsync(userId)).ToDictionary(category => category.Id);

        return (await transactions.SearchAsync(userId, TransactionFilter.All with { From = range.From, To = range.To }))
            .Select(transaction =>
            {
                var category = categoryById[transaction.CategoryId];
                return new ReportEntry(
                    transaction,
                    currencyByAccount[transaction.AccountId],
                    category,
                    category.ParentId is { } parentId ? categoryById[parentId] : null);
            })
            .ToList();
    }
}

/// <param name="From">The earliest date to include.</param>
/// <param name="To">The latest date to include.</param>
public sealed record DateRange(DateOnly From, DateOnly To);

/// <summary>A Transaction with the Currency of its Account, its Category, and that Category's parent.</summary>
public sealed record ReportEntry(Transaction Transaction, Currency Currency, Category Category, Category? Parent)
{
    /// <summary>
    /// What the Transaction adds to its Category's total: money earned in an Income Category, money spent in an
    /// Expense Category. Money going the other way subtracts, so a Refund reduces the spending it refunds.
    /// </summary>
    public decimal Amount => Category.Kind is CategoryKind.Income ? Transaction.SignedAmount : -Transaction.SignedAmount;
}
