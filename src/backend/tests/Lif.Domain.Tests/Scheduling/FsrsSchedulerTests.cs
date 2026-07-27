using Lif.Domain.Scheduling;

namespace Lif.Domain.Tests.Scheduling;

public class FsrsSchedulerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ReviewRating.Again, 0.4)]
    [InlineData(ReviewRating.Hard, 0.6)]
    [InlineData(ReviewRating.Good, 2.4)]
    [InlineData(ReviewRating.Easy, 5.8)]
    public void FirstReview_UsesInitialStability(ReviewRating rating, double expectedStability)
    {
        var state = FsrsScheduler.Next(null, rating, T0);

        state.Stability.Should().BeApproximately(expectedStability, 1e-12);
        state.LastReviewAt.Should().Be(T0);
        state.DueAt.Should().Be(T0.AddDays(expectedStability));
    }

    [Fact]
    public void FirstReview_HarderRatingsYieldHigherDifficulty()
    {
        var again = FsrsScheduler.Next(null, ReviewRating.Again, T0);
        var good = FsrsScheduler.Next(null, ReviewRating.Good, T0);
        var easy = FsrsScheduler.Next(null, ReviewRating.Easy, T0);

        again.Difficulty.Should().BeGreaterThan(good.Difficulty);
        good.Difficulty.Should().BeGreaterThan(easy.Difficulty);
    }

    [Fact]
    public void SubsequentReview_IntervalsAreMonotonic_EasyGeGoodGeHard()
    {
        var first = FsrsScheduler.Next(null, ReviewRating.Good, T0);
        var reviewAt = T0.AddDays(first.Stability);

        var hard = FsrsScheduler.Next(first, ReviewRating.Hard, reviewAt);
        var good = FsrsScheduler.Next(first, ReviewRating.Good, reviewAt);
        var easy = FsrsScheduler.Next(first, ReviewRating.Easy, reviewAt);

        easy.Stability.Should().BeGreaterThanOrEqualTo(good.Stability);
        good.Stability.Should().BeGreaterThanOrEqualTo(hard.Stability);
    }

    [Fact]
    public void SuccessfulReview_GrowsStability()
    {
        var first = FsrsScheduler.Next(null, ReviewRating.Good, T0);

        var next = FsrsScheduler.Next(first, ReviewRating.Good, T0.AddDays(first.Stability));

        next.Stability.Should().BeGreaterThan(first.Stability);
    }

    [Fact]
    public void Lapse_ShrinksStability_ButKeepsAFloor()
    {
        var grown = new FsrsState(Stability: 40.0, Difficulty: 5.0, T0, T0.AddDays(40));

        var lapsed = FsrsScheduler.Next(grown, ReviewRating.Again, T0.AddDays(40));

        lapsed.Stability.Should().BeLessThan(grown.Stability);
        lapsed.Stability.Should().BeGreaterThanOrEqualTo(0.3);
    }

    [Fact]
    public void LapseOfTinyStability_HitsTheFloor()
    {
        var tiny = new FsrsState(Stability: 0.1, Difficulty: 8.0, T0, T0.AddDays(0.1));

        var lapsed = FsrsScheduler.Next(tiny, ReviewRating.Again, T0.AddDays(1));

        lapsed.Stability.Should().BeApproximately(0.3, 1e-12);
    }

    [Fact]
    public void LateReview_GrowsStabilityMoreThanEarlyReview()
    {
        var first = FsrsScheduler.Next(null, ReviewRating.Good, T0);

        var early = FsrsScheduler.Next(first, ReviewRating.Good, T0.AddDays(first.Stability * 0.2));
        var late = FsrsScheduler.Next(first, ReviewRating.Good, T0.AddDays(first.Stability * 3.0));

        late.Stability.Should().BeGreaterThan(early.Stability);
    }

    [Fact]
    public void Difficulty_StaysWithinFsrsBounds()
    {
        var state = FsrsScheduler.Next(null, ReviewRating.Again, T0);
        for (var i = 0; i < 20; i++)
        {
            state = FsrsScheduler.Next(state, ReviewRating.Again, state.DueAt);
            state.Difficulty.Should().BeInRange(1.0, 10.0);
        }

        for (var i = 0; i < 20; i++)
        {
            state = FsrsScheduler.Next(state, ReviewRating.Easy, state.DueAt);
            state.Difficulty.Should().BeInRange(1.0, 10.0);
        }
    }

    [Fact]
    public void Retrievability_IsNinetyPercentAtStabilityDays()
    {
        var state = FsrsScheduler.Next(null, ReviewRating.Good, T0);

        state.Retrievability(T0.AddDays(state.Stability)).Should().BeApproximately(0.9, 1e-9);
    }

    [Fact]
    public void Retrievability_DecaysOverTime()
    {
        var state = FsrsScheduler.Next(null, ReviewRating.Good, T0);

        var atDue = state.Retrievability(state.DueAt);
        var wayLater = state.Retrievability(state.DueAt.AddDays(30));

        wayLater.Should().BeLessThan(atDue);
        state.Retrievability(T0).Should().BeApproximately(1.0, 1e-12);
    }
}
