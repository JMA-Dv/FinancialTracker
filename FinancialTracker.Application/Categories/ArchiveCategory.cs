using FinancialTracker.Domain.Categories;

namespace FinancialTracker.Application.Categories;

public sealed class ArchiveCategory(ICategoryStore categories)
{
    public async Task<Category> ExecuteAsync(Guid userId, Guid categoryId, bool archived)
    {
        var category = await categories.GetAsync(userId, categoryId);
        if (archived)
        {
            category.Archive();
        }
        else
        {
            category.Unarchive();
        }

        await categories.SaveChangesAsync();
        return category;
    }
}
