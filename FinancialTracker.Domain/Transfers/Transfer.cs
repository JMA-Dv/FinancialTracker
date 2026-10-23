using FinancialTracker.Domain.Accounts;
using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.Transfers;

/// <summary>A movement of an amount from one of a User's Accounts to another. Never income or spending.</summary>
public sealed class Transfer
{
    public const int MaxNoteLength = 500;

    private Transfer(Guid id, Guid userId, Guid fromAccountId, Guid toAccountId, DateOnly date, decimal amountSent, decimal amountReceived, string? note)
    {
        Id = id;
        UserId = userId;
        FromAccountId = fromAccountId;
        ToAccountId = toAccountId;
        Date = date;
        AmountSent = amountSent;
        AmountReceived = amountReceived;
        Note = note;
    }

    public Guid Id { get; private set; }

    /// <summary>The User who owns this Transfer. Nobody else can see or change it.</summary>
    public Guid UserId { get; private set; }

    public Guid FromAccountId { get; private set; }

    public Guid ToAccountId { get; private set; }

    public DateOnly Date { get; private set; }

    /// <summary>What left the from-Account, in its Currency. Always positive.</summary>
    public decimal AmountSent { get; private set; }

    /// <summary>What arrived in the to-Account, in its Currency. Always positive.</summary>
    public decimal AmountReceived { get; private set; }

    public string? Note { get; private set; }

    /// <summary>How the Transfer changes the Account's balance: negative for the from-Account, positive for the to-Account, zero otherwise.</summary>
    public decimal EffectOn(Guid accountId) =>
        accountId == FromAccountId ? -AmountSent : accountId == ToAccountId ? AmountReceived : 0m;

    /// <param name="receivedAmount">
    /// What arrived in the to-Account. Between Accounts in different Currencies it is required, since nothing is
    /// converted (ADR 0003). In one Currency it can be left out, and otherwise must equal the amount.
    /// </param>
    /// <param name="today">Today's date, which a Transfer can't be after. Earlier dates are fine, even before an Opening Balance.</param>
    /// <exception cref="DomainValidationException">The Accounts, date, amounts or note break a rule.</exception>
    /// <exception cref="DomainConflictException">Either Account is closed.</exception>
    public static Transfer Create(
        Account from, Account to, DateOnly date, decimal amount, decimal? receivedAmount, string? note, DateOnly today)
    {
        var received = EnsureValid(from, to, date, amount, receivedAmount, note, today);
        return new Transfer(Guid.CreateVersion7(), from.UserId, from.Id, to.Id, date, amount, received, note);
    }

    /// <summary>Replaces every field. Nothing changes unless all of it is valid.</summary>
    /// <param name="currentFrom">The Account the Transfer leaves now.</param>
    /// <param name="currentTo">The Account the Transfer arrives in now.</param>
    /// <param name="from">The Account it will leave, which may be the same.</param>
    /// <param name="to">The Account it will arrive in, which may be the same.</param>
    /// <exception cref="DomainValidationException">The Accounts, date, amounts or note break a rule.</exception>
    /// <exception cref="DomainConflictException">Any of the four Accounts is closed.</exception>
    public void Edit(
        Account currentFrom,
        Account currentTo,
        Account from,
        Account to,
        DateOnly date,
        decimal amount,
        decimal? receivedAmount,
        string? note,
        DateOnly today)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(currentFrom.Id, FromAccountId);
        ArgumentOutOfRangeException.ThrowIfNotEqual(currentTo.Id, ToAccountId);

        // Moving money off a Closed Account would leave it holding a balance.
        currentFrom.EnsureOpen();
        currentTo.EnsureOpen();

        var received = EnsureValid(from, to, date, amount, receivedAmount, note, today);
        FromAccountId = from.Id;
        ToAccountId = to.Id;
        Date = date;
        AmountSent = amount;
        AmountReceived = received;
        Note = note;
    }

    // Returns the amount that arrived.
    private static decimal EnsureValid(
        Account from, Account to, DateOnly date, decimal amount, decimal? receivedAmount, string? note, DateOnly today)
    {
        from.EnsureOpen();
        to.EnsureOpen();

        if (from.Id == to.Id)
        {
            throw new DomainValidationException("Same Account", "A Transfer needs two different Accounts.");
        }

        if (date > today)
        {
            throw new DomainValidationException("Date in the future", $"{date:yyyy-MM-dd} is after today ({today:yyyy-MM-dd}).");
        }

        if (amount <= 0)
        {
            throw new DomainValidationException("Invalid amount", "A Transfer's amount must be more than zero.");
        }

        from.Currency.EnsureValidAmount(amount);

        if (note?.Length > MaxNoteLength)
        {
            throw new DomainValidationException("Note too long", $"A note can have at most {MaxNoteLength} characters.");
        }

        if (from.Currency == to.Currency)
        {
            if (receivedAmount is not null && receivedAmount != amount)
            {
                throw new DomainValidationException(
                    "Received amount differs",
                    $"Both Accounts are in {from.Currency.Code}, so the same amount arrives as leaves. Leave the received amount out.");
            }

            return amount;
        }

        if (receivedAmount is not { } received)
        {
            throw new DomainValidationException(
                "Received amount required",
                $"{from.Name} is in {from.Currency.Code} and {to.Name} in {to.Currency.Code}. Say how much arrived, since nothing is converted.");
        }

        if (received <= 0)
        {
            throw new DomainValidationException("Invalid received amount", "The amount that arrived must be more than zero.");
        }

        to.Currency.EnsureValidAmount(received);
        return received;
    }
}
