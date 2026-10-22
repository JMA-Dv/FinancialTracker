using FinancialTracker.Domain.Categories;

namespace FinancialTracker.Application.Categories;

/// <summary>Adds a top-level Category, or a child under an existing one.</summary>
public sealed class CreateCategory(ICategoryStore categories)
{
    /// <param name="kind">Required for a top-level Category. A child takes its parent's kind, and may name it.</param>
    public async Task<Category> ExecuteAsync(Guid userId, string name, CategoryKind? kind, Guid? parentId)
    {
        var parent = parentId is { } id ? await categories.GetAsync(userId, id) : null;
        var category = Category.Create(userId, name, kind, parent);

        await categories.EnsureFreeAsync(userId, category.ParentId, category.Name, exceptId: null);
        await categories.AddAsync(category);
        return category;
    }
}
