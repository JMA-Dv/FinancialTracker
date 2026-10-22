using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Money;
using FinancialTracker.Domain.Transactions;
using FinancialTracker.Domain.Transfers;

namespace FinancialTracker.Domain.Accounts;

/// <summary>A place where a User's money sits, kept in one Currency (ADR 0003).</summary>
public sealed class Account
{
    public const int MaxNameLength = 100;

    private readonly List<BalanceAdjustment> balanceAdjustments = [];

    private Account(Guid id, Guid userId, string name, AccountKind kind, Currency currency)
    {
        Id = id;
        UserId = userId;
        Name = name;
        Kind = kind;
        Currency = currency;
    }

    public Guid Id { get; private set; }

    /// <summary>The User who owns this Account. Nobody else can see or change it.</summary>
    public Guid UserId { get; private set; }

    public string Name { get; private set; }

    public AccountKind Kind { get; private set; }

    public Currency Currency { get; private set; }

    /// <summary>A Liability Account's balance is normally money the User owes. Every other Account is an asset.</summary>
    public bool IsLiability => Kind is AccountKind.CreditCard;

    public IReadOnlyCollection<BalanceAdjustment> BalanceAdjustments => balanceAdjustments;

    /// <summary>The earliest Balance Adjustment, or null if the Account starts at zero.</summary>
    public BalanceAdjustment? OpeningBalance => balanceAdjustments.MinBy(adjustment => adjustment.Date);

    /// <param name="openingBalance">What the Account held at the end of the date tracking starts, or null to start at zero.</param>
    /// <param name="today">Today's date, which the Opening Balance can't be after.</param>
    /// <exception cref="DomainValidationException">The name, or the Opening Balance, breaks a rule.</exception>
    public static Account Open(
        Guid userId, string name, AccountKind kind, Currency currency, OpeningBalance? openingBalance, DateOnly today)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new DomainValidationException("Unknown Account kind", $"'{kind}' is not a kind of Account.");
        }

        var account = new Account(Guid.CreateVersion7(), userId, ValidName(name), kind, currency);
        if (openingBalance is not null)
        {
            account.EnsureValidMoney(openingBalance.Balance, openingBalance.Date, today);
            account.balanceAdjustments.Add(
                new BalanceAdjustment(Guid.CreateVersion7(), openingBalance.Date, openingBalance.Balance));
        }

        return account;
    }

    /// <exception cref="DomainValidationException">The name is blank or too long.</exception>
    public void Rename(string name) => Name = ValidName(name);

    /// <summary>
    /// Changes the Currency the Account's amounts are in. Nothing is converted (ADR 0003), so once money has moved
    /// through the Account its Currency is locked: changing it would relabel that money.
    /// </summary>
    /// <param name="hasTransactionsOrTransfers">Whether any Transaction or Transfer uses this Account.</param>
    /// <exception cref="DomainConflictException">The Account already has Transactions or Transfers.</exception>
    /// <exception cref="DomainValidationException">A Balance Adjustment has more decimal places than the new Currency allows.</exception>
    public void ChangeCurrency(Currency currency, bool hasTransactionsOrTransfers)
    {
        if (currency == Currency)
        {
            return;
        }

        if (hasTransactionsOrTransfers)
        {
            throw new DomainConflictException(
                "Account Currency locked",
                $"{Name} already has Transactions or Transfers in {Currency}, and amounts are never relabelled or converted.");
        }

        foreach (var adjustment in balanceAdjustments)
        {
            currency.EnsureValidAmount(adjustment.Balance);
        }

        Currency = currency;
    }

    /// <summary>
    /// The Account's balance at the end of the date: the latest Balance Adjustment on or before it, or zero
    /// without one, plus the Transactions and Transfers dated after that Adjustment up to the date. Anything dated
    /// on or before the Adjustment is already inside the balance it states. Balances are always derived, never stored.
    /// </summary>
    /// <param name="transactions">Transactions to count. Those on other Accounts are ignored.</param>
    /// <param name="transfers">Transfers to count. Those that don't touch this Account are ignored.</param>
    public decimal BalanceOn(DateOnly date, IEnumerable<Transaction> transactions, IEnumerable<Transfer> transfers)
    {
        var adjustment = balanceAdjustments
            .Where(a => a.Date <= date)
            .MaxBy(a => a.Date);

        bool Counts(DateOnly movedOn) => movedOn <= date && (adjustment is null || movedOn > adjustment.Date);

        var movedSince =
            transactions.Where(t => t.AccountId == Id && Counts(t.Date)).Sum(t => t.SignedAmount)
            + transfers.Where(t => Counts(t.Date)).Sum(t => t.EffectOn(Id));

        return (adjustment?.Balance ?? 0m) + movedSince;
    }

    // Balances only include money that has actually moved, and amounts can't be more precise than the Currency.
    private void EnsureValidMoney(decimal amount, DateOnly date, DateOnly today)
    {
        if (date > today)
        {
            throw new DomainValidationException("Date in the future", $"{date:yyyy-MM-dd} is after today ({today:yyyy-MM-dd}).");
        }

        Currency.EnsureValidAmount(amount);
    }

    private static string ValidName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length is 0 or > MaxNameLength)
        {
            throw new DomainValidationException(
                "Invalid Account name", $"An Account's name must have 1 to {MaxNameLength} characters.");
        }

        return trimmed;
    }
}

/// <summary>What an Account held at the end of the date the User started tracking it.</summary>
public sealed record OpeningBalance(decimal Balance, DateOnly Date);
