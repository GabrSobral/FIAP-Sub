using FiapSub.Core.Domain.Exceptions;
using FiapSub.Core.Domain.ValueObjects;

namespace FiapSub.Core.Domain.Tests.ValueObjects;

public class PaymentCodeTests
{
    [Fact]
    public void Create_TrimsSurroundingWhitespace()
    {
        PaymentCode.Create("  PAY-123_abc  ").Value.ShouldBe("PAY-123_abc");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlank(string? value)
    {
        Should.Throw<DomainValidationException>(() => PaymentCode.Create(value))
            .Message.ShouldContain("required");
    }

    [Fact]
    public void Create_RejectsOverlyLongCode()
    {
        Should.Throw<DomainValidationException>(() => PaymentCode.Create(new string('a', PaymentCode.MaxLength + 1)))
            .Message.ShouldContain("at most 64 characters");
    }

    [Theory]
    [InlineData("pay 123")]
    [InlineData("pay/123")]
    [InlineData("pay.123")]
    [InlineData("pay#123")]
    public void Create_RejectsDisallowedCharacters(string value)
    {
        Should.Throw<DomainValidationException>(() => PaymentCode.Create(value))
            .Message.ShouldContain("only letters, digits");
    }

    [Fact]
    public void Equality_IsOrdinalOnTheValue()
    {
        var a = PaymentCode.Create("PAY-1");
        var b = PaymentCode.Create("PAY-1");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
        a.GetHashCode().ShouldBe(b.GetHashCode());
        a.ShouldNotBe(PaymentCode.Create("pay-1"));
        a.Equals((object?)null).ShouldBeFalse();
        ((PaymentCode?)null == null).ShouldBeTrue();
        (a == null).ShouldBeFalse();
    }

    [Fact]
    public void ToString_ReturnsTheRawValue()
    {
        PaymentCode.Create("PAY-1").ToString().ShouldBe("PAY-1");
    }
}
