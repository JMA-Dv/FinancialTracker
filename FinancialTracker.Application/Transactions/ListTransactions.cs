using FinancialTracker.Application.Payees;
using FinancialTracker.Domain.Transactions;

namespace FinancialTracker.Application.Transactions;

/// <summary>Lists the User's Transactions, newest first.</summary>
public sealed class ListTransactions(ITransactionStore transactions, IPayeeStore payees)
{
    public async Task<IReadOnlyList<Transaction>> ExecuteAsync(Guid userId, TransactionFilter filter)
    {
        // A Payee that isn't theirs is not found, rather than an empty list that hints it exists.
        if (filter.PayeeId is { } payeeId)
        {
            await payees.GetAsync(userId, payeeId);
        }

        return await transactions.SearchAsync(userId, filter);
    }
}
