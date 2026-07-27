using Lif.Domain.Learner;
using Lif.Domain.Scheduling;

namespace Lif.Domain.Tests.Learner;

public class WordStateTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);

    private static WordState MeetWord() => WordState.Meet(Guid.CreateVersion7(), Guid.CreateVersion7(), WordSource.Lesson);

    private static FsrsState StateWithStability(double stability) =>
        new(stability, 5.0, Now, Now.AddDays(stability));

    [Fact]
    public void Meet_StartsAsNew()
    {
        var word = MeetWord();

        word.Status.Should().Be(WordStatus.New);
        word.LapseCount.Should().Be(0);
        word.Leech.Should().BeFalse();
    }

    [Theory]
    [InlineData(1.0, WordStatus.Learning)]
    [InlineData(7.0, WordStatus.Known)]
    [InlineData(29.9, WordStatus.Known)]
    [InlineData(30.0, WordStatus.Mastered)]
    public void SuccessfulReview_MapsStabilityToStatus(double stability, WordStatus expected)
    {
        var word = MeetWord();

        word.ApplyReview(ReviewRating.Good, StateWithStability(stability), Now);

        word.Status.Should().Be(expected);
        word.LastReviewAt.Should().Be(Now);
    }

    [Fact]
    public void CrossingMasteredThreshold_RaisesWordMastered_Once()
    {
        var word = MeetWord();

        word.ApplyReview(ReviewRating.Good, StateWithStability(31), Now);
        word.ApplyReview(ReviewRating.Good, StateWithStability(45), Now.AddDays(31));

        word.DomainEvents.OfType<WordMastered>().Should().HaveCount(1);
    }

    [Fact]
    public void Lapse_FromKnown_MovesToRelearning_AndRaisesWordLapsed()
    {
        var word = MeetWord();
        word.ApplyReview(ReviewRating.Good, StateWithStability(10), Now);
        word.Status.Should().Be(WordStatus.Known);

        word.ApplyReview(ReviewRating.Again, StateWithStability(0.5), Now.AddDays(10));

        word.Status.Should().Be(WordStatus.Relearning);
        word.LapseCount.Should().Be(1);
        word.DomainEvents.OfType<WordLapsed>().Should().HaveCount(1);
    }

    [Fact]
    public void Lapse_WhileStillLearning_StaysInLearning()
    {
        var word = MeetWord();

        word.ApplyReview(ReviewRating.Again, StateWithStability(0.4), Now);

        word.Status.Should().Be(WordStatus.Learning);
        word.LapseCount.Should().Be(1);
    }

    [Fact]
    public void EightLapses_FlagTheWordAsLeech()
    {
        var word = MeetWord();

        for (var i = 0; i < WordState.LeechThreshold; i++)
        {
            word.Leech.Should().BeFalse();
            word.ApplyReview(ReviewRating.Again, StateWithStability(0.4), Now.AddDays(i));
        }

        word.Leech.Should().BeTrue();
        word.LapseCount.Should().Be(WordState.LeechThreshold);
    }

    [Fact]
    public void RelearnedWord_CanRecoverToKnown()
    {
        var word = MeetWord();
        word.ApplyReview(ReviewRating.Good, StateWithStability(10), Now);
        word.ApplyReview(ReviewRating.Again, StateWithStability(0.5), Now.AddDays(10));

        word.ApplyReview(ReviewRating.Good, StateWithStability(8), Now.AddDays(11));

        word.Status.Should().Be(WordStatus.Known);
    }
}
