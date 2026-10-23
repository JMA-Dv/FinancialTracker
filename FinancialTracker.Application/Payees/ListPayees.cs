using FinancialTracker.Application.Transactions;

namespace FinancialTracker.Application.Payees;

/// <summary>Lists the User's Payees by name, each with the Category to suggest for it.</summary>
public sealed class ListPayees(IPayeeStore payees, ITransactionStore transactions)
{
    /// <summary>The User's Payees ordered by name, ignoring case.</summary>
    public async Task<IReadOnlyList<PayeeSummary>> ExecuteAsync(Guid userId)
    {
        var suggestions = await transactions.LatestCategoriesByPayeeAsync(userId, payeeId: null);
        return (await payees.ListAsync(userId))
            .OrderBy(payee => payee.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(payee => PayeeSummary.Of(payee, suggestions))
            .ToList();
    }
}
