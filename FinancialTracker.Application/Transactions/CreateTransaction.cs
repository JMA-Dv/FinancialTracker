using FinancialTracker.Application.Accounts;
using FinancialTracker.Application.Categories;
using FinancialTracker.Domain.Transactions;

namespace FinancialTracker.Application.Transactions;

/// <summary>Records money going out of, or coming into, one of the User's Accounts.</summary>
public sealed class CreateTransaction(IAccountStore accounts, ICategoryStore categories, ITransactionStore transactions, TimeProvider clock)
{
    public async Task<Transaction> ExecuteAsync(Guid userId, TransactionDetails request)
    {
        var account = await accounts.GetAsync(userId, request.AccountId);
        var category = await categories.GetAsync(userId, request.CategoryId);
        var transaction = Transaction.Create(
            account, request.Date, request.Direction, request.Amount, category, request.Note, clock.Today());
        await transactions.AddAsync(transaction);
        return transaction;
    }
}
