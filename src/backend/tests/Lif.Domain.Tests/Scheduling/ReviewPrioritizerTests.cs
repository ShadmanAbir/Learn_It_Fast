using Lif.Domain.Scheduling;

namespace Lif.Domain.Tests.Scheduling;

public class ReviewPrioritizerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);

    private static FsrsState DueDaysAgo(double days, double stability = 5.0) =>
        new(stability, 5.0, Now.AddDays(-days - stability), Now.AddDays(-days));

    [Fact]
    public void DefaultWeights_SumToOne()
    {
        var w = ReviewPriorityWeights.Default;

        (w.Overdueness + w.RecentErrorRate + w.Importance + w.ForgettingRisk + w.Uncertainty)
            .Should().BeApproximately(1.0, 1e-12);
    }

    [Fact]
    public void Score_IsAlwaysWithinUnitInterval()
    {
        var worst = ReviewPrioritizer.Score(DueDaysAgo(1000, 0.5), Now, recentErrorRate: 1, importance: 1, confidence: 0);
        var best = ReviewPrioritizer.Score(DueDaysAgo(-5), Now, recentErrorRate: 0, importance: 0, confidence: 1);

        worst.Should().BeLessThanOrEqualTo(1.0);
        best.Should().BeGreaterThanOrEqualTo(0.0);
    }

    [Fact]
    public void MoreOverdueItems_ScoreHigher()
    {
        var slightlyOverdue = ReviewPrioritizer.Score(DueDaysAgo(1), Now, 0.2, 0.5, 0.5);
        var veryOverdue = ReviewPrioritizer.Score(DueDaysAgo(10), Now, 0.2, 0.5, 0.5);

        veryOverdue.Should().BeGreaterThan(slightlyOverdue);
    }

    [Fact]
    public void HigherErrorRate_ScoresHigher()
    {
        var stable = ReviewPrioritizer.Score(DueDaysAgo(1), Now, recentErrorRate: 0.0, importance: 0.5, confidence: 0.5);
        var shaky = ReviewPrioritizer.Score(DueDaysAgo(1), Now, recentErrorRate: 0.8, importance: 0.5, confidence: 0.5);

        shaky.Should().BeGreaterThan(stable);
    }

    [Fact]
    public void LowerConfidence_ScoresHigher()
    {
        var confident = ReviewPrioritizer.Score(DueDaysAgo(1), Now, 0.2, 0.5, confidence: 0.9);
        var uncertain = ReviewPrioritizer.Score(DueDaysAgo(1), Now, 0.2, 0.5, confidence: 0.1);

        uncertain.Should().BeGreaterThan(confident);
    }

    [Fact]
    public void CustomWeights_OverrideDefaults()
    {
        var onlyImportance = new ReviewPriorityWeights(0, 0, 1.0, 0, 0);

        var score = ReviewPrioritizer.Score(DueDaysAgo(50), Now, 1.0, importance: 0.42, confidence: 0.0, onlyImportance);

        score.Should().BeApproximately(0.42, 1e-12);
    }
}
