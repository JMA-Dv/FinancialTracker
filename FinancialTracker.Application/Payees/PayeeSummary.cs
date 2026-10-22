using FinancialTracker.Domain.Payees;

namespace FinancialTracker.Application.Payees;

/// <param name="SuggestedCategoryId">The Category of the User's most recent Transaction with the Payee, or null if there is none.</param>
public sealed record PayeeSummary(Payee Payee, Guid? SuggestedCategoryId)
{
    /// <param name="latestCategories">Each Payee's latest Category, with no entry for a Payee without Transactions.</param>
    internal static PayeeSummary Of(Payee payee, IReadOnlyDictionary<Guid, Guid> latestCategories) =>
        new(payee, latestCategories.TryGetValue(payee.Id, out var categoryId) ? categoryId : null);
}
