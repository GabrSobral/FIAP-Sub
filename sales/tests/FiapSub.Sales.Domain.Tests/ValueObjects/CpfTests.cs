using FiapSub.Sales.Domain.Exceptions;
using FiapSub.Sales.Domain.ValueObjects;

namespace FiapSub.Sales.Domain.Tests.ValueObjects;

public class CpfTests
{
    [Theory]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    [InlineData(" 529 982 247 25 ")]
    [InlineData("11144477735")]
    [InlineData("12345678909")]
    [InlineData("39874561246")]
    public void Create_AcceptsAValidCpfInAnyFormatting(string value)
    {
        Cpf.Create(value).Value.Length.ShouldBe(Cpf.Length);
    }

    [Fact]
    public void Create_StripsTheMask()
    {
        Cpf.Create("529.982.247-25").Value.ShouldBe("52998224725");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Create_RejectsBlank(string? value)
    {
        Should.Throw<DomainValidationException>(() => Cpf.Create(value)).Message.ShouldContain("required");
    }

    [Theory]
    [InlineData("5299822472")]
    [InlineData("529982247250")]
    [InlineData("abc")]
    public void Create_RejectsTheWrongNumberOfDigits(string value)
    {
        Should.Throw<DomainValidationException>(() => Cpf.Create(value)).Message.ShouldContain("11 digits");
    }

    [Theory]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("99999999999")]
    public void Create_RejectsRepeatedDigits(string value)
    {
        Should.Throw<DomainValidationException>(() => Cpf.Create(value))
            .Message.ShouldContain("identical digits");
    }

    [Theory]
    [InlineData("52998224726")]
    [InlineData("52998224715")]
    [InlineData("12345678901")]
    public void Create_RejectsABadCheckDigit(string value)
    {
        Should.Throw<DomainValidationException>(() => Cpf.Create(value))
            .Message.ShouldContain("check digit");
    }

    [Fact]
    public void Masked_AndRedacted_FormatForDisplay()
    {
        var cpf = Cpf.Create("52998224725");

        cpf.Masked.ShouldBe("529.982.247-25");
        cpf.Redacted.ShouldBe("***.***.**7-25");
        cpf.ToString().ShouldBe("529.982.247-25");
    }

    [Fact]
    public void Equality_IsByDigits()
    {
        var a = Cpf.Create("529.982.247-25");
        var b = Cpf.Create("52998224725");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
        a.GetHashCode().ShouldBe(b.GetHashCode());
        a.ShouldNotBe(Cpf.Create("11144477735"));
        a.Equals((object?)null).ShouldBeFalse();
        ((Cpf?)null == null).ShouldBeTrue();
        (a == null).ShouldBeFalse();
    }
}
