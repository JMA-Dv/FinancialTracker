using FinancialTracker.Domain.Common;

namespace FinancialTracker.Application.Categories;

internal static class CategoryNames
{
    /// <exception cref="DomainConflictException">A sibling already has the name, whatever its case.</exception>
    public static async Task EnsureFreeAsync(
        this ICategoryStore categories, Guid userId, Guid? parentId, string name, Guid? exceptId)
    {
        var trimmed = name.Trim();
        if (await categories.NameTakenAsync(userId, parentId, trimmed, exceptId))
        {
            throw new DomainConflictException("Category name taken", $"There is already a Category called {trimmed} here.");
        }
    }
}
