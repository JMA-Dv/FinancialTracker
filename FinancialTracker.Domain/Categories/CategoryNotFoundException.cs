using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.Categories;

public sealed class CategoryNotFoundException(Guid categoryId)
    : DomainNotFoundException("Category not found", $"You have no Category {categoryId}.");
