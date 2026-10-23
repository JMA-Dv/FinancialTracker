using System.ComponentModel.DataAnnotations;
using FinancialTracker.Api.Features.Auth;
using FinancialTracker.Application.Reports;
using FinancialTracker.Domain.Categories;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTracker.Api.Features.Reports;

[ApiController]
[Route("api/reports")]
public class ReportsController(
    GetNetWorth getNetWorth,
    GetIncomeAndSpending getIncomeAndSpending,
    GetCategoryBreakdown getCategoryBreakdown,
    GetPayeeSpending getPayeeSpending)
    : ControllerBase
{
    /// <summary>What the User's Accounts held at the end of a date, per Currency. Defaults to today; a future date is a 400.</summary>
    [HttpGet("net-worth")]
    public async Task<NetWorthResponse> NetWorth(DateOnly? date) => NetWorthResponse.Of(await getNetWorth.ExecuteAsync(User.Id(), date));

    /// <summary>
    /// Income and spending over a date range, per Currency. Only Transactions count, so Transfers and Balance Adjustments
    /// never show up, and a Refund reduces the spending in its Category.
    /// </summary>
    [HttpGet("income-spending")]
    public async Task<IncomeSpendingResponse> IncomeSpending([FromQuery] DateRangeRequest range) =>
        IncomeSpendingResponse.Of(await getIncomeAndSpending.ExecuteAsync(User.Id(), range.ToRange()));

    /// <summary>
    /// Income and spending over a date range by Category, per Currency. A parent Category's total includes its own
    /// Transactions and all its children's. Archived Categories are included.
    /// </summary>
    [HttpGet("categories")]
    public async Task<CategoryBreakdownResponse> Categories([FromQuery] DateRangeRequest range) =>
        CategoryBreakdownResponse.Of(await getCategoryBreakdown.ExecuteAsync(User.Id(), range.ToRange()));

    /// <summary>
    /// Spending over a date range by Payee, per Currency. Income doesn't count, and a Refund reduces what was spent with its Payee.
    /// Spending on Transactions without a Payee is listed last, with no Payee.
    /// </summary>
    [HttpGet("payees")]
    public async Task<PayeeSpendingResponse> Payees([FromQuery] DateRangeRequest range) =>
        PayeeSpendingResponse.Of(await getPayeeSpending.ExecuteAsync(User.Id(), range.ToRange()));
}

/// <param name="From">The earliest date to include.</param>
/// <param name="To">The latest date to include. Not before <paramref name="From"/>.</param>
public sealed record DateRangeRequest([Required] DateOnly? From, [Required] DateOnly? To)
{
    public DateRange ToRange() =>
        new(
            From ?? throw new InvalidOperationException("From is checked by model validation."),
            To ?? throw new InvalidOperationException("To is checked by model validation."));
}

public sealed record CurrencyTotalResponse(string Currency, decimal Amount)
{
    public static CurrencyTotalResponse Of(CurrencyTotal total) => new(total.Currency.Code, total.Amount);
}

public sealed record NetWorthResponse(DateOnly Date, IReadOnlyList<CurrencyTotalResponse> Totals)
{
    public static NetWorthResponse Of(NetWorth netWorth) => new(netWorth.Date, netWorth.Totals.Select(CurrencyTotalResponse.Of).ToList());
}

public sealed record IncomeSpendingTotalResponse(string Currency, decimal Income, decimal Spending)
{
    public static IncomeSpendingTotalResponse Of(IncomeSpendingTotal total) => new(total.Currency.Code, total.Income, total.Spending);
}

public sealed record IncomeSpendingResponse(DateOnly From, DateOnly To, IReadOnlyList<IncomeSpendingTotalResponse> Totals)
{
    public static IncomeSpendingResponse Of(IncomeAndSpending report) =>
        new(report.Range.From, report.Range.To, report.Totals.Select(IncomeSpendingTotalResponse.Of).ToList());
}

/// <param name="Total">Earned in an Income Category, spent in an Expense Category. For a parent, its own plus all its children's.</param>
/// <param name="OwnAmount">What was recorded directly on this Category, leaving its children out.</param>
public sealed record CategoryTotalResponse(
    Guid Id, string Name, CategoryKind Kind, bool IsArchived, decimal Total, decimal OwnAmount, IReadOnlyList<CategoryTotalResponse> Children)
{
    public static CategoryTotalResponse Of(CategoryTotal total) =>
        new(
            total.Category.Id,
            total.Category.Name,
            total.Category.Kind,
            total.Category.IsArchived,
            total.Total,
            total.OwnAmount,
            total.Children.Select(Of).ToList());
}

public sealed record CategoryCurrencyResponse(string Currency, IReadOnlyList<CategoryTotalResponse> Categories);

public sealed record CategoryBreakdownResponse(DateOnly From, DateOnly To, IReadOnlyList<CategoryCurrencyResponse> Currencies)
{
    public static CategoryBreakdownResponse Of(CategoryBreakdown report) =>
        new(
            report.Range.From,
            report.Range.To,
            report.Currencies
                .Select(currency => new CategoryCurrencyResponse(currency.Currency.Code, currency.Categories.Select(CategoryTotalResponse.Of).ToList()))
                .ToList());
}

/// <param name="PayeeId">Null for spending on Transactions that name no Payee.</param>
public sealed record PayeeTotalResponse(Guid? PayeeId, string? Name, decimal Spending)
{
    public static PayeeTotalResponse Of(PayeeTotal total) => new(total.PayeeId, total.Name, total.Spending);
}

public sealed record PayeeCurrencyResponse(string Currency, IReadOnlyList<PayeeTotalResponse> Payees);

public sealed record PayeeSpendingResponse(DateOnly From, DateOnly To, IReadOnlyList<PayeeCurrencyResponse> Currencies)
{
    public static PayeeSpendingResponse Of(PayeeSpending report) =>
        new(
            report.Range.From,
            report.Range.To,
            report.Currencies
                .Select(currency => new PayeeCurrencyResponse(currency.Currency.Code, currency.Payees.Select(PayeeTotalResponse.Of).ToList()))
                .ToList());
}
