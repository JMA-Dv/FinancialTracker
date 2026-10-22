namespace FinancialTracker.Application.Transactions;

/// <summary>Removes one of the User's Transactions, and with it its effect on the Account's balance.</summary>
public sealed class DeleteTransaction(ITransactionStore transactions)
{
    public async Task ExecuteAsync(Guid userId, Guid transactionId) =>
        await transactions.RemoveAsync(await transactions.GetAsync(userId, transactionId));
}
