using FinancialTracker.Application.Transactions;

namespace FinancialTracker.Application.Payees;

/// <summary>Renames one of the User's Payees, which changes it on every Transaction that uses it.</summary>
public sealed class RenamePayee(IPayeeStore payees, ITransactionStore transactions)
{
    public async Task<PayeeSummary> ExecuteAsync(Guid userId, Guid payeeId, string name)
    {
        var payee = await payees.GetAsync(userId, payeeId);
        payee.Rename(name);

        await payees.EnsureFreeAsync(userId, payee.Name, exceptId: payee.Id);
        await payees.SaveChangesAsync();

        var suggestions = await transactions.LatestCategoriesByPayeeAsync(userId, payee.Id);
        return PayeeSummary.Of(payee, suggestions);
    }
}
