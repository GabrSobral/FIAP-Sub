using FiapSub.Core.Application.Common;

namespace FiapSub.Core.Application.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void Failure_CarriesTheError()
    {
        var error = Error.Conflict("x.y", "nope");

        var result = Result.Failure(error);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void GenericSuccess_ExposesTheValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void GenericFailure_ThrowsWhenTheValueIsRead()
    {
        var result = Result.Failure<int>(Error.NotFound("a.b", "gone"));

        result.IsFailure.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => result.Value)
            .Message.ShouldContain("carries no value");
    }

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    public void ErrorFactories_SetTheType(ErrorType type)
    {
        var error = type switch
        {
            ErrorType.Validation => Error.Validation("c", "m"),
            ErrorType.NotFound => Error.NotFound("c", "m"),
            _ => Error.Conflict("c", "m"),
        };

        error.Type.ShouldBe(type);
        error.Code.ShouldBe("c");
        error.Message.ShouldBe("m");
    }
}
