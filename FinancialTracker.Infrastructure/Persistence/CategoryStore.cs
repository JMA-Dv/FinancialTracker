using FinancialTracker.Application.Categories;
using FinancialTracker.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace FinancialTracker.Infrastructure.Persistence;

public sealed class CategoryStore(FinanceDbContext dbContext) : ICategoryStore
{
    public async Task AddAsync(Category category)
    {
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();
    }

    public Task<Category?> FindAsync(Guid userId, Guid categoryId) =>
        dbContext.Categories.SingleOrDefaultAsync(category => category.Id == categoryId && category.UserId == userId);

    public async Task<IReadOnlyList<Category>> ListAsync(Guid userId) =>
        await dbContext.Categories.Where(category => category.UserId == userId).ToListAsync();

    public Task<bool> HasChildrenAsync(Guid categoryId) =>
        dbContext.Categories.AnyAsync(category => category.ParentId == categoryId);

    public Task<bool> NameTakenAsync(Guid userId, Guid? parentId, string name, Guid? exceptId)
    {
        var lowered = name.ToLower();
        return dbContext.Categories.AnyAsync(category =>
            category.UserId == userId
            && category.ParentId == parentId
            && category.Id != exceptId
            && category.Name.ToLower() == lowered);
    }

    public Task SaveChangesAsync() => dbContext.SaveChangesAsync();
}
