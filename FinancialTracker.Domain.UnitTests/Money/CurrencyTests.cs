using System.Globalization;
using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Money;

namespace FinancialTracker.Domain.UnitTests.Money;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData("USD", "10.05")]
    [InlineData("USD", "10.50")]
    [InlineData("USD", "10")]
    [InlineData("MXN", "1234.99")]
    [InlineData("JPY", "1050")]
    [InlineData("JPY", "1050.00")]
    [InlineData("KWD", "1.125")]
    public void An_amount_within_the_Currencys_decimal_places_is_accepted(string code, string amount) =>
        Currency.FromCode(code).EnsureValidAmount(decimal.Parse(amount, CultureInfo.InvariantCulture));

    [Theory]
    [InlineData("USD", "10.005")]
    [InlineData("MXN", "0.001")]
    [InlineData("JPY", "10.5")]
    [InlineData("KWD", "1.1255")]
    public void An_amount_with_more_decimal_places_than_the_Currency_allows_is_rejected(string code, string amount)
    {
        var currency = Currency.FromCode(code);

        var rejected = Assert.Throws<DomainValidationException>(() => currency.EnsureValidAmount(decimal.Parse(amount, CultureInfo.InvariantCulture)));

        Assert.Contains(code, rejected.Message);
    }

    [Theory]
    [InlineData("1000000000000000")]
    [InlineData("-1000000000000000")]
    public void An_amount_too_large_for_the_database_is_rejected(string amount) =>
        Assert.Throws<DomainValidationException>(() => Currency.FromCode("USD").EnsureValidAmount(decimal.Parse(amount, CultureInfo.InvariantCulture)));

    [Fact]
    public void The_largest_amount_the_database_holds_is_accepted() =>
        Currency.FromCode("KWD").EnsureValidAmount(-999_999_999_999_999.999m);
}
