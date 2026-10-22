using FinancialTracker.Domain.Categories;

namespace FinancialTracker.Application.Categories;

/// <summary>Moves a Category under a different parent, or to the top level.</summary>
public sealed class MoveCategory(ICategoryStore categories)
{
    public async Task<Category> ExecuteAsync(Guid userId, Guid categoryId, Guid? parentId)
    {
        var category = await categories.GetAsync(userId, categoryId);
        var parent = parentId is { } id ? await categories.GetAsync(userId, id) : null;

        category.MoveUnder(parent, await categories.HasChildrenAsync(category.Id));

        await categories.EnsureFreeAsync(userId, category.ParentId, category.Name, exceptId: category.Id);
        await categories.SaveChangesAsync();
        return category;
    }
}
