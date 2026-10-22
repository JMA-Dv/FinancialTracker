using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.Categories;

/// <summary>
/// The label that says what money was for or where it came from. A Category is either Income or Expense and has
/// at most one parent, which must be a top-level Category of the same kind: there are two levels, no more.
/// </summary>
public sealed class Category
{
    public const int MaxNameLength = 100;

    private Category(Guid id, Guid userId, string name, CategoryKind kind, Guid? parentId)
    {
        Id = id;
        UserId = userId;
        Name = name;
        Kind = kind;
        ParentId = parentId;
    }

    public Guid Id { get; private set; }

    /// <summary>The User who owns this Category. Nobody else can see or change it.</summary>
    public Guid UserId { get; private set; }

    public string Name { get; private set; }

    public CategoryKind Kind { get; private set; }

    public Guid? ParentId { get; private set; }

    /// <summary>Hidden from new Transactions, but kept on the ones that already use it.</summary>
    public bool IsArchived { get; private set; }

    /// <param name="kind">Income or Expense. Leave it out for a child, which takes its parent's kind.</param>
    /// <param name="parent">The Category to sit under, or null for a top-level Category.</param>
    /// <exception cref="DomainValidationException">The name, the kind or the parent breaks a rule.</exception>
    public static Category Create(Guid userId, string name, CategoryKind? kind, Category? parent)
    {
        ValidName(name);
        if (parent is null)
        {
            if (kind is not { } topLevelKind || !Enum.IsDefined(topLevelKind))
            {
                throw new DomainValidationException("Category kind required", "A top-level Category must be Income or Expense.");
            }

            return new Category(Guid.CreateVersion7(), userId, ValidName(name), topLevelKind, parentId: null);
        }

        if (kind is { } given && given != parent.Kind)
        {
            throw KindMismatch(parent);
        }

        EnsureCanHaveChildren(parent);
        return new Category(Guid.CreateVersion7(), userId, ValidName(name), parent.Kind, parent.Id);
    }

    /// <exception cref="DomainValidationException">The name is blank or too long.</exception>
    public void Rename(string name) => Name = ValidName(name);

    /// <summary>Moves the Category under another one, or to the top level when parent is null.</summary>
    /// <param name="hasChildren">Whether other Categories sit under this one.</param>
    /// <exception cref="DomainValidationException">The move would make a third level, or mix Income with Expense.</exception>
    public void MoveUnder(Category? parent, bool hasChildren)
    {
        if (parent is not null)
        {
            if (parent.Id == Id)
            {
                throw new DomainValidationException("Invalid parent", "A Category can't sit under itself.");
            }

            if (hasChildren)
            {
                throw new DomainValidationException(
                    "Category has children", $"{Name} has Categories under it, so moving it under another would make a third level.");
            }

            EnsureCanHaveChildren(parent);
            if (parent.Kind != Kind)
            {
                throw KindMismatch(parent);
            }
        }

        ParentId = parent?.Id;
    }

    public void Archive() => IsArchived = true;

    public void Unarchive() => IsArchived = false;

    private static void EnsureCanHaveChildren(Category parent)
    {
        if (parent.ParentId is not null)
        {
            throw new DomainValidationException(
                "Too many levels", $"{parent.Name} is already under another Category, and Categories have two levels at most.");
        }
    }

    private static DomainValidationException KindMismatch(Category parent) =>
        new("Category kind mismatch", $"A Category under {parent.Name} must be {parent.Kind}.");

    private static string ValidName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length is 0 or > MaxNameLength)
        {
            throw new DomainValidationException(
                "Invalid Category name", $"A Category's name must have 1 to {MaxNameLength} characters.");
        }

        return trimmed;
    }
}
