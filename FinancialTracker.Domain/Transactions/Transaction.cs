using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Categories;
using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.Transactions;

/// <summary>One amount of money into or out of one Account, on a date, in one Category.</summary>
public sealed class Transaction
{
    public const int MaxNoteLength = 500;

    private Transaction(
        Guid id, Guid userId, Guid accountId, DateOnly date, TransactionDirection direction, decimal amount, Guid categoryId, string? note)
    {
        Id = id;
        UserId = userId;
        AccountId = accountId;
        Date = date;
        Direction = direction;
        Amount = amount;
        CategoryId = categoryId;
        Note = note;
    }

    public Guid Id { get; private set; }

    /// <summary>The User who owns this Transaction. Nobody else can see or change it.</summary>
    public Guid UserId { get; private set; }

    public Guid AccountId { get; private set; }

    public DateOnly Date { get; private set; }

    public TransactionDirection Direction { get; private set; }

    /// <summary>Always positive, in the Account's Currency. <see cref="Direction"/> says which way it moves.</summary>
    public decimal Amount { get; private set; }

    public Guid CategoryId { get; private set; }

    public string? Note { get; private set; }

    /// <summary>The amount as it changes the Account's balance: negative for Out.</summary>
    public decimal SignedAmount => Direction is TransactionDirection.In ? Amount : -Amount;

    /// <param name="today">Today's date, which a Transaction can't be after. Earlier dates are fine, even before the Opening Balance.</param>
    /// <exception cref="DomainValidationException">The date, direction, amount or note breaks a rule.</exception>
    /// <exception cref="DomainConflictException">The Category is archived.</exception>
    public static Transaction Create(
        Account account,
        DateOnly date,
        TransactionDirection direction,
        decimal amount,
        Category category,
        string? note,
        DateOnly today)
    {
        EnsureValid(account, date, direction, amount, note, today);
        EnsureCategoryUsable(category);
        return new Transaction(Guid.CreateVersion7(), account.UserId, account.Id, date, direction, amount, category.Id, note);
    }

    /// <summary>Replaces every field. Nothing changes unless all of it is valid.</summary>
    /// <param name="currentAccount">The Account the Transaction is on now.</param>
    /// <param name="account">The Account it will be on, which may be the same.</param>
    /// <exception cref="DomainValidationException">A field breaks a rule, or the new Account has another Currency.</exception>
    /// <exception cref="DomainConflictException">The Category is archived and isn't the one the Transaction already has.</exception>
    public void Edit(
        Account currentAccount,
        Account account,
        DateOnly date,
        TransactionDirection direction,
        decimal amount,
        Category category,
        string? note,
        DateOnly today)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(currentAccount.Id, AccountId);

        // Nothing is ever converted (ADR 0003), so money can't change Currency by changing Account.
        if (account.Currency != currentAccount.Currency)
        {
            throw new DomainValidationException(
                "Account Currency differs",
                $"{account.Name} is in {account.Currency.Code} but this Transaction is in {currentAccount.Currency.Code}, and amounts are never converted.");
        }

        EnsureValid(account, date, direction, amount, note, today);

        // A Category archived after the Transaction used it stays valid for it.
        if (category.Id != CategoryId)
        {
            EnsureCategoryUsable(category);
        }

        AccountId = account.Id;
        Date = date;
        Direction = direction;
        Amount = amount;
        CategoryId = category.Id;
        Note = note;
    }

    private static void EnsureValid(
        Account account, DateOnly date, TransactionDirection direction, decimal amount, string? note, DateOnly today)
    {
        if (!Enum.IsDefined(direction))
        {
            throw new DomainValidationException("Unknown direction", $"'{direction}' is not a direction of money.");
        }

        if (date > today)
        {
            throw new DomainValidationException("Date in the future", $"{date:yyyy-MM-dd} is after today ({today:yyyy-MM-dd}).");
        }

        if (amount <= 0)
        {
            throw new DomainValidationException("Invalid amount", "A Transaction's amount must be more than zero. Use the direction for money going out.");
        }

        account.Currency.EnsureValidAmount(amount);

        if (note?.Length > MaxNoteLength)
        {
            throw new DomainValidationException("Note too long", $"A note can have at most {MaxNoteLength} characters.");
        }
    }

    private static void EnsureCategoryUsable(Category category)
    {
        if (category.IsArchived)
        {
            throw new DomainConflictException("Category archived", $"{category.Name} is archived. Unarchive it or pick another Category.");
        }
    }
}
