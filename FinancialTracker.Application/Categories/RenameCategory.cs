using FinancialTracker.Domain.Categories;

namespace FinancialTracker.Application.Categories;

public sealed class RenameCategory(ICategoryStore categories)
{
    public async Task<Category> ExecuteAsync(Guid userId, Guid categoryId, string name)
    {
        var category = await categories.GetAsync(userId, categoryId);
        category.Rename(name);

        await categories.EnsureFreeAsync(userId, category.ParentId, category.Name, exceptId: category.Id);
        await categories.SaveChangesAsync();
        return category;
    }
}
