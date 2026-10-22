using FinancialTracker.Domain.Categories;
using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.UnitTests.Categories;

public sealed class CategoryTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void A_top_level_Category_needs_a_kind()
    {
        Assert.Throws<DomainValidationException>(() => Category.Create(UserId, "Food", kind: null, parent: null));
    }

    [Fact]
    public void A_child_takes_its_parents_kind_when_none_is_given()
    {
        var child = Category.Create(UserId, "Groceries", kind: null, Food());

        Assert.Equal(CategoryKind.Expense, child.Kind);
    }

    [Fact]
    public void A_child_with_a_different_kind_from_its_parent_is_rejected()
    {
        Assert.Throws<DomainValidationException>(() => Category.Create(UserId, "Bonus", CategoryKind.Income, Food()));
    }

    [Fact]
    public void A_third_level_is_rejected()
    {
        var groceries = Category.Create(UserId, "Groceries", null, Food());

        Assert.Throws<DomainValidationException>(() => Category.Create(UserId, "Organic", null, groceries));
    }

    [Fact]
    public void A_blank_name_is_rejected()
    {
        Assert.Throws<DomainValidationException>(() => Category.Create(UserId, "  ", CategoryKind.Expense, parent: null));
    }

    [Fact]
    public void A_name_over_the_limit_is_rejected()
    {
        var name = new string('a', Category.MaxNameLength + 1);

        Assert.Throws<DomainValidationException>(() => Category.Create(UserId, name, CategoryKind.Expense, parent: null));
    }

    [Fact]
    public void A_name_is_trimmed()
    {
        Assert.Equal("Food", Category.Create(UserId, " Food ", CategoryKind.Expense, parent: null).Name);
    }

    [Fact]
    public void A_Category_without_children_can_move_under_a_top_level_Category_of_its_kind()
    {
        var rent = Category.Create(UserId, "Rent", CategoryKind.Expense, parent: null);
        var housing = Category.Create(UserId, "Housing", CategoryKind.Expense, parent: null);

        rent.MoveUnder(housing, hasChildren: false);

        Assert.Equal(housing.Id, rent.ParentId);
    }

    [Fact]
    public void A_child_can_move_to_the_top_level()
    {
        var groceries = Category.Create(UserId, "Groceries", null, Food());

        groceries.MoveUnder(parent: null, hasChildren: false);

        Assert.Null(groceries.ParentId);
    }

    [Fact]
    public void A_Category_with_children_cant_move_under_another_Category()
    {
        var food = Food();
        var housing = Category.Create(UserId, "Housing", CategoryKind.Expense, parent: null);

        Assert.Throws<DomainValidationException>(() => food.MoveUnder(housing, hasChildren: true));
        Assert.Null(food.ParentId);
    }

    [Fact]
    public void A_Category_cant_move_under_a_child_under_itself_or_under_another_kind()
    {
        var food = Food();
        var groceries = Category.Create(UserId, "Groceries", null, food);
        var rent = Category.Create(UserId, "Rent", CategoryKind.Expense, parent: null);
        var salary = Category.Create(UserId, "Salary", CategoryKind.Income, parent: null);

        Assert.Throws<DomainValidationException>(() => rent.MoveUnder(groceries, hasChildren: false));
        Assert.Throws<DomainValidationException>(() => rent.MoveUnder(rent, hasChildren: false));
        Assert.Throws<DomainValidationException>(() => rent.MoveUnder(salary, hasChildren: false));
        Assert.Null(rent.ParentId);
    }

    [Fact]
    public void Nothing_can_be_created_or_moved_under_an_archived_Category()
    {
        var food = Food();
        var rent = Category.Create(UserId, "Rent", CategoryKind.Expense, parent: null);
        food.Archive();

        Assert.Throws<DomainConflictException>(() => Category.Create(UserId, "Snacks", kind: null, food));
        Assert.Throws<DomainConflictException>(() => rent.MoveUnder(food, hasChildren: false));
    }

    [Fact]
    public void A_Category_can_be_archived_and_unarchived()
    {
        var food = Food();

        food.Archive();
        Assert.True(food.IsArchived);

        food.Unarchive();
        Assert.False(food.IsArchived);
    }

    [Fact]
    public void The_starter_set_is_the_default_top_level_Categories_with_only_Income_as_Income()
    {
        var starter = StarterCategories.CreateFor(UserId);

        Assert.Equal(
            ["Dining", "Groceries", "Shopping", "Entertainment", "Bills", "Gifts", "Work", "Income", "Health", "Investments", "Lend"],
            starter.Select(category => category.Name));
        Assert.All(starter, category => Assert.Equal(UserId, category.UserId));
        Assert.All(starter, category => Assert.Null(category.ParentId));
        Assert.Equal(["Income"], starter.Where(category => category.Kind is CategoryKind.Income).Select(category => category.Name));
    }

    private static Category Food() => Category.Create(UserId, "Food", CategoryKind.Expense, parent: null);
}
