using FinancialTracker.Application.Transactions;
using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Payees;

namespace FinancialTracker.Application.Payees;

/// <summary>Renames one of the User's Payees, which changes it on every Transaction that uses it.</summary>
public sealed class RenamePayee(IPayeeStore payees, ITransactionStore transactions)
{
    /// <exception cref="PayeeNotFoundException">The Payee doesn't exist or belongs to another User.</exception>
    /// <exception cref="DomainValidationException">The name is blank or too long.</exception>
    /// <exception cref="DomainConflictException">Another Payee already has the name, whatever its case.</exception>
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
