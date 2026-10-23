using FinancialTracker.Application.Accounts;

namespace FinancialTracker.Application.Transactions;

/// <summary>Removes one of the User's Transactions, and with it its effect on the Account's balance.</summary>
public sealed class DeleteTransaction(IAccountStore accounts, ITransactionStore transactions)
{
    public async Task ExecuteAsync(Guid userId, Guid transactionId)
    {
        var transaction = await transactions.GetAsync(userId, transactionId);

        // A Closed Account holds a zero balance, which removing money would change.
        (await accounts.GetAsync(userId, transaction.AccountId)).EnsureOpen();
        await transactions.RemoveAsync(transaction);
    }
}
