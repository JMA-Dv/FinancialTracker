using FinancialTracker.Application.Categories;
using FinancialTracker.Application.Payees;
using FinancialTracker.Domain.Transactions;

namespace FinancialTracker.Application.Transactions;

/// <summary>Lists the User's Transactions, newest first.</summary>
public sealed class ListTransactions(ITransactionStore transactions, ICategoryStore categories, IPayeeStore payees)
{
    public async Task<IReadOnlyList<Transaction>> ExecuteAsync(Guid userId, TransactionFilter filter)
    {
        // A Category or Payee that isn't theirs is not found, rather than an empty list that hints it exists.
        if (filter.CategoryId is { } categoryId)
        {
            await categories.GetAsync(userId, categoryId);
        }

        if (filter.PayeeId is { } payeeId)
        {
            await payees.GetAsync(userId, payeeId);
        }

        return await transactions.SearchAsync(userId, filter);
    }
}
