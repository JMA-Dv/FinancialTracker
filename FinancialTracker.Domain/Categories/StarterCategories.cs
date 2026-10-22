namespace FinancialTracker.Domain.Categories;

/// <summary>The ready-made Categories every new User starts with. They are the User's own, and fully editable.</summary>
public static class StarterCategories
{
    private static readonly (string Name, CategoryKind Kind)[] Set =
    [
        ("Dining", CategoryKind.Expense),
        ("Groceries", CategoryKind.Expense),
        ("Shopping", CategoryKind.Expense),
        ("Entertainment", CategoryKind.Expense),
        ("Bills", CategoryKind.Expense),
        ("Gifts", CategoryKind.Expense),
        ("Work", CategoryKind.Expense),
        ("Income", CategoryKind.Income),
        ("Health", CategoryKind.Expense),
        ("Investments", CategoryKind.Expense),
        ("Lend", CategoryKind.Expense),
    ];

    /// <summary>A fresh copy of the starter set for the User.</summary>
    public static IReadOnlyList<Category> CreateFor(Guid userId) =>
        Set.Select(category => Category.Create(userId, category.Name, category.Kind, parent: null)).ToList();
}
