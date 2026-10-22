using FinancialTracker.Application.Accounts;
using FinancialTracker.Application.Categories;
using FinancialTracker.Domain.Transactions;

namespace FinancialTracker.Application.Transactions;

/// <summary>Replaces every field of one of the User's Transactions.</summary>
public sealed class EditTransaction(
    IAccountStore accounts, ICategoryStore categories, ITransactionStore transactions, TimeProvider clock)
{
    public async Task<Transaction> ExecuteAsync(Guid userId, Guid transactionId, TransactionDetails details)
    {
        var transaction = await transactions.GetAsync(userId, transactionId);
        var currentAccount = await accounts.GetAsync(userId, transaction.AccountId);
        var account = details.AccountId == currentAccount.Id ? currentAccount : await accounts.GetAsync(userId, details.AccountId);
        var category = await categories.GetAsync(userId, details.CategoryId);

        transaction.Edit(currentAccount, account, details.Date, details.Direction, details.Amount, category, details.Note, clock.Today());
        await transactions.SaveChangesAsync();
        return transaction;
    }
}
