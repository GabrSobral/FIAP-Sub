using FiapSub.Core.Domain.Exceptions;
using FiapSub.Core.Domain.ValueObjects;

namespace FiapSub.Core.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_DefaultsToBrl()
    {
        var money = Money.Create(1500.50m);

        money.Amount.ShouldBe(1500.50m);
        money.Currency.ShouldBe("BRL");
        money.IsGreaterThanZero.ShouldBeTrue();
    }

    [Fact]
    public void Create_NormalizesCurrencyCasingAndWhitespace()
    {
        Money.Create(10m, " usd ").Currency.ShouldBe("USD");
    }

    [Fact]
    public void Create_AcceptsZero()
    {
        var money = Money.Create(0m);

        money.Amount.ShouldBe(0m);
        money.IsGreaterThanZero.ShouldBeFalse();
    }

    [Fact]
    public void Create_RejectsNegativeAmount()
    {
        Should.Throw<DomainValidationException>(() => Money.Create(-0.01m))
            .Message.ShouldContain("cannot be negative");
    }

    [Fact]
    public void Create_RejectsMoreThanTwoDecimalPlaces()
    {
        Should.Throw<DomainValidationException>(() => Money.Create(10.001m))
            .Message.ShouldContain("2 decimal places");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_RejectsBlankCurrency(string? currency)
    {
        Should.Throw<DomainValidationException>(() => Money.Create(10m, currency!));
    }

    [Theory]
    [InlineData("BR")]
    [InlineData("BRLL")]
    [InlineData("BR1")]
    public void Create_RejectsMalformedCurrency(string currency)
    {
        Should.Throw<DomainValidationException>(() => Money.Create(10m, currency))
            .Message.ShouldContain("ISO 4217");
    }

    [Fact]
    public void Equality_IsByAmountAndCurrency()
    {
        var a = Money.Create(99.90m);
        var b = Money.Create(99.90m);
        var differentAmount = Money.Create(99.91m);
        var differentCurrency = Money.Create(99.90m, "USD");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
        a.GetHashCode().ShouldBe(b.GetHashCode());
        a.ShouldNotBe(differentAmount);
        a.ShouldNotBe(differentCurrency);
        a.Equals((object?)null).ShouldBeFalse();
        (a == null).ShouldBeFalse();
        ((Money?)null == null).ShouldBeTrue();
    }

    [Fact]
    public void ToString_IsCultureInvariant()
    {
        Money.Create(1234.5m).ToString().ShouldBe("BRL 1234.50");
    }

    [Fact]
    public void Zero_IsZeroBrl()
    {
        Money.Zero.Amount.ShouldBe(0m);
        Money.Zero.Currency.ShouldBe("BRL");
    }
}
