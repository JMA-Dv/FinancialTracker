using FinancialTracker.Domain.Transactions;

namespace FinancialTracker.Application.Transactions;

/// <summary>Lists the User's Transactions, newest first.</summary>
public sealed class ListTransactions(ITransactionStore transactions)
{
    public async Task<IReadOnlyList<Transaction>> ExecuteAsync(Guid userId, TransactionFilter filter) =>
        await transactions.SearchAsync(userId, filter);
}
