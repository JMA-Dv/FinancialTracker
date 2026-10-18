using System.Diagnostics.CodeAnalysis;
using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.Money;

/// <summary>
/// The unit of money an Account's amounts are kept in (ADR 0003), identified by its ISO 4217 code.
/// </summary>
public sealed record Currency
{
    private Currency(string code, int minorUnits)
    {
        Code = code;
        MinorUnits = minorUnits;
    }

    /// <summary>The ISO 4217 alphabetic code, such as USD.</summary>
    public string Code { get; }

    /// <summary>How many decimal places an amount may have: 2 for USD, 0 for JPY.</summary>
    public int MinorUnits { get; }

    /// <summary>The Currency with the ISO 4217 code, ignoring case.</summary>
    /// <exception cref="DomainValidationException">The code isn't an ISO 4217 Currency.</exception>
    public static Currency FromCode(string code) =>
        TryFromCode(code, out var currency)
            ? currency
            : throw new DomainValidationException(
                "Unknown Currency",
                $"'{code}' is not an ISO 4217 currency code, such as USD, MXN or JPY.");

    /// <summary>Finds the Currency with the ISO 4217 code, ignoring case.</summary>
    public static bool TryFromCode(string? code, [NotNullWhen(true)] out Currency? currency)
    {
        currency = null;
        var normalized = code?.Trim().ToUpperInvariant();
        if (normalized is null || !Iso4217.MinorUnitsByCode.TryGetValue(normalized, out var minorUnits))
        {
            return false;
        }

        currency = new Currency(normalized, minorUnits);
        return true;
    }

    /// <exception cref="DomainValidationException">
    /// The amount has more decimal places than this Currency allows, such as 10.005 USD or 10.5 JPY.
    /// </exception>
    public void EnsureValidAmount(decimal amount)
    {
        // Trailing zeros don't count: 10.50 USD and 1050.00 JPY are fine.
        if (decimal.Round(amount, MinorUnits) != amount)
        {
            throw new DomainValidationException(
                "Too many decimal places",
                $"{amount} has more decimal places than {Code} allows ({MinorUnits}).");
        }
    }

    public override string ToString() => Code;
}
