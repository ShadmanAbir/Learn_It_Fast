using Lif.Domain.Learning;
using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Tests.Learning;

public class DifficultyGovernorTests
{
    [Fact]
    public void FewerThanFiveOutcomes_NeverAdjusts()
    {
        var current = Difficulty.From(5);

        DifficultyGovernor.Adjust(current, [1.0, 1.0, 1.0, 1.0]).Should().Be(current);
    }

    [Fact]
    public void HighSuccessRate_StepsUpOneBand()
    {
        DifficultyGovernor.Adjust(Difficulty.From(5), [1.0, 1.0, 1.0, 1.0, 0.5])
            .Should().Be(Difficulty.From(6));
    }

    [Fact]
    public void LowSuccessRate_StepsDownOneBand()
    {
        DifficultyGovernor.Adjust(Difficulty.From(5), [0.0, 0.5, 0.5, 1.0, 0.5])
            .Should().Be(Difficulty.From(4));
    }

    [Fact]
    public void ComfortZone_LeavesDifficultyUnchanged()
    {
        DifficultyGovernor.Adjust(Difficulty.From(5), [1.0, 1.0, 0.5, 0.5, 0.5])
            .Should().Be(Difficulty.From(5)); // 0.70 sits between the thresholds
    }

    [Fact]
    public void BoundaryRates_MatchThresholdSemantics()
    {
        // Exactly 0.85 steps up; exactly 0.60 holds. Exact binary fractions avoid FP drift.
        DifficultyGovernor.Adjust(Difficulty.From(5), [1.0, 1.0, 1.0, 1.0, 0.25])
            .Should().Be(Difficulty.From(6)); // avg = 0.85
        DifficultyGovernor.Adjust(Difficulty.From(5), [0.5, 0.5, 0.5, 0.75, 0.75])
            .Should().Be(Difficulty.From(5)); // avg = 0.60
    }

    [Fact]
    public void NeverMovesMoreThanOneBand_AndRespectsScaleEdges()
    {
        DifficultyGovernor.Adjust(Difficulty.From(10), [1.0, 1.0, 1.0, 1.0, 1.0])
            .Should().Be(Difficulty.From(10));
        DifficultyGovernor.Adjust(Difficulty.From(1), [0.0, 0.0, 0.0, 0.0, 0.0])
            .Should().Be(Difficulty.From(1));
    }
}
