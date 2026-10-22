namespace FinancialTracker.Domain.Categories;

/// <summary>The ready-made Categories every new User starts with. They are the User's own, and fully editable.</summary>
public static class StarterCategories
{
    private static readonly (string Name, CategoryKind Kind, string[] Children)[] Set =
    [
        ("Food", CategoryKind.Expense, ["Groceries", "Restaurants"]),
        ("Housing", CategoryKind.Expense, ["Rent", "Utilities"]),
        ("Transport", CategoryKind.Expense, ["Fuel", "Public transport"]),
        ("Health", CategoryKind.Expense, []),
        ("Entertainment", CategoryKind.Expense, []),
        ("Shopping", CategoryKind.Expense, []),
        ("Salary", CategoryKind.Income, []),
        ("Other income", CategoryKind.Income, []),
    ];

    /// <summary>A fresh copy of the starter set for the User, parents before their children.</summary>
    public static IReadOnlyList<Category> CreateFor(Guid userId)
    {
        var categories = new List<Category>();
        foreach (var (name, kind, children) in Set)
        {
            var parent = Category.Create(userId, name, kind, parent: null);
            categories.Add(parent);
            categories.AddRange(children.Select(child => Category.Create(userId, child, kind: null, parent)));
        }

        return categories;
    }
}
