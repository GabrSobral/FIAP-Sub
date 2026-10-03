using FiapSub.Core.Domain.Exceptions;
using FiapSub.Core.Domain.ValueObjects;

namespace FiapSub.Core.Domain.Tests.ValueObjects;

public class ModelYearTests
{
    [Theory]
    [InlineData(1900)]
    [InlineData(2024)]
    [InlineData(2027)]
    public void Create_AcceptsYearsInsideTheWindow(int year)
    {
        ModelYear.Create(year, 2027).Value.ShouldBe(year);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(0)]
    [InlineData(2028)]
    public void Create_RejectsYearsOutsideTheWindow(int year)
    {
        Should.Throw<DomainValidationException>(() => ModelYear.Create(year, 2027))
            .Message.ShouldContain("Model year must be between 1900 and 2027");
    }

    [Fact]
    public void Restore_SkipsTheUpperBoundCheck()
    {
        ModelYear.Restore(1850).Value.ShouldBe(1850);
    }

    [Fact]
    public void Equality_IsByValue()
    {
        ModelYear.Create(2020, 2027).ShouldBe(ModelYear.Create(2020, 2027));
        ModelYear.Create(2020, 2027).ShouldNotBe(ModelYear.Create(2021, 2027));
    }

    [Fact]
    public void ToString_IsCultureInvariant()
    {
        ModelYear.Create(2020, 2027).ToString().ShouldBe("2020");
    }
}
