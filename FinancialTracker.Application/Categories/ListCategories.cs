using FinancialTracker.Domain.Categories;

namespace FinancialTracker.Application.Categories;

/// <summary>The User's Categories as a tree: top-level Categories, each with the Categories under it.</summary>
public sealed class ListCategories(ICategoryStore categories)
{
    /// <param name="includeArchived">
    /// Whether to show Archived Categories. Without it an Archived Category is hidden together with what is under it.
    /// </param>
    public async Task<IReadOnlyList<CategoryNode>> ExecuteAsync(Guid userId, bool includeArchived)
    {
        var visible = (await categories.ListAsync(userId)).Where(category => includeArchived || !category.IsArchived).ToList();
        var childrenByParent = visible.Where(category => category.ParentId is not null).ToLookup(category => category.ParentId!.Value);

        return visible
            .Where(category => category.ParentId is null)
            .OrderBy(category => category.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(parent => new CategoryNode(
                parent,
                childrenByParent[parent.Id]
                    .OrderBy(child => child.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(child => new CategoryNode(child, []))
                    .ToList()))
            .ToList();
    }
}

public sealed record CategoryNode(Category Category, IReadOnlyList<CategoryNode> Children);
