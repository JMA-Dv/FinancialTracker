using FinancialTracker.Domain.Common;

namespace FinancialTracker.Domain.Money;

/// <summary>Every amount is in the User's Currency, so nothing can be recorded until one is chosen (ADR 0001).</summary>
public sealed class CurrencyNotChosenException()
    : DomainConflictException(
        "Currency not chosen",
        "Choose your Currency in user settings before recording money.");
