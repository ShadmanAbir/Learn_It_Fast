using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Tests.Primitives;

public class DifficultyTests
{
    [Fact]
    public void From_RejectsOutOfRangeValues()
    {
        FluentActions.Invoking(() => Difficulty.From(0)).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => Difficulty.From(11)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Step_NeverMovesMoreThanOneBand_AndClampsAtBounds()
    {
        Difficulty.From(5).StepUp().Value.Should().Be(6);
        Difficulty.From(5).StepDown().Value.Should().Be(4);
        Difficulty.From(Difficulty.Max).StepUp().Value.Should().Be(Difficulty.Max);
        Difficulty.From(Difficulty.Min).StepDown().Value.Should().Be(Difficulty.Min);
    }
}
