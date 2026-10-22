using FinancialTracker.Domain.Categories;

namespace FinancialTracker.Application.Categories;

/// <summary>Where Categories are kept. Every read is scoped to the User who owns the Category.</summary>
public interface ICategoryStore
{
    Task AddAsync(Category category);

    /// <summary>The User's Category, or null if it doesn't exist or belongs to another User.</summary>
    Task<Category?> FindAsync(Guid userId, Guid categoryId);

    /// <summary>All of the User's Categories, Archived ones included.</summary>
    Task<IReadOnlyList<Category>> ListAsync(Guid userId);

    /// <summary>Whether other Categories sit under the Category.</summary>
    Task<bool> HasChildrenAsync(Guid categoryId);

    /// <summary>
    /// Whether the User has a Category with the name under the parent (top level when null), ignoring case.
    /// <paramref name="exceptId"/> is left out, so a Category doesn't clash with itself.
    /// </summary>
    Task<bool> NameTakenAsync(Guid userId, Guid? parentId, string name, Guid? exceptId);

    /// <summary>Saves changes made to Categories found through this store.</summary>
    Task SaveChangesAsync();
}

public static class CategoryStoreExtensions
{
    /// <exception cref="CategoryNotFoundException">The Category doesn't exist or belongs to another User.</exception>
    public static async Task<Category> GetAsync(this ICategoryStore categories, Guid userId, Guid categoryId) =>
        await categories.FindAsync(userId, categoryId) ?? throw new CategoryNotFoundException(categoryId);
}
