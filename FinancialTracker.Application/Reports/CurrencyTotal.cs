using FinancialTracker.Domain.Money;

namespace FinancialTracker.Application.Reports;

/// <summary>An amount in one Currency. Reports keep Currencies apart and never add them together (ADR 0003).</summary>
public sealed record CurrencyTotal(Currency Currency, decimal Amount);
