using FinancialTracker.Domain.Common;
using FinancialTracker.Domain.Payees;

namespace FinancialTracker.Domain.UnitTests.Payees;

public sealed class PayeeTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void A_name_is_trimmed()
    {
        Assert.Equal("Joe's Diner", Payee.Create(UserId, "  Joe's Diner ").Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_name_is_rejected(string name)
    {
        Assert.Throws<DomainValidationException>(() => Payee.Create(UserId, name));
    }

    [Fact]
    public void A_name_over_the_limit_is_rejected_on_create_and_rename()
    {
        var name = new string('a', Payee.MaxNameLength + 1);
        var payee = Payee.Create(UserId, "Bakery");

        Assert.Throws<DomainValidationException>(() => Payee.Create(UserId, name));
        Assert.Throws<DomainValidationException>(() => payee.Rename(name));
        Assert.Equal("Bakery", payee.Name);
    }
}
