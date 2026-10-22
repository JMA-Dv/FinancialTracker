using FinancialTracker.Application.Accounts;
using FinancialTracker.Application.Categories;
using FinancialTracker.Application.Payees;
using FinancialTracker.Domain.Transactions;

namespace FinancialTracker.Application.Transactions;

/// <summary>Replaces every field of one of the User's Transactions.</summary>
public sealed class EditTransaction(
    IAccountStore accounts, ICategoryStore categories, IPayeeStore payees, ITransactionStore transactions, TimeProvider clock)
{
    public async Task<Transaction> ExecuteAsync(Guid userId, Guid transactionId, TransactionDetails details)
    {
        var transaction = await transactions.GetAsync(userId, transactionId);
        var currentAccount = await accounts.GetAsync(userId, transaction.AccountId);
        var account = details.AccountId == currentAccount.Id ? currentAccount : await accounts.GetAsync(userId, details.AccountId);
        var category = await categories.GetAsync(userId, details.CategoryId);
        var payee = await payees.ResolveAsync(userId, details.PayeeName);

        transaction.Edit(currentAccount, account, details.Date, details.Direction, details.Amount, category, payee?.Payee, details.Note, clock.Today());
        await payees.SaveIfNewAsync(payee);
        await transactions.SaveChangesAsync();
        return transaction;
    }
}
