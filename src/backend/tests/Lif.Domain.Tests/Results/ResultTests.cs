using Lif.SharedKernel.Results;

namespace Lif.Domain.Tests.Results;

public class ResultTests
{
    [Fact]
    public void Success_CarriesValue_AndNoError()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ExposesError_AndThrowsOnValueAccess()
    {
        var error = Error.Validation("TEST_CODE", "message");
        var result = Result.Failure<int>(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        FluentActions.Invoking(() => result.Value).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitConversion_WrapsValueAsSuccess()
    {
        Result<string> result = "ok";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
    }
}
