using Lif.Domain.Learner;

namespace Lif.Domain.Tests.Learner;

public class ConceptMasteryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);

    private static ConceptMastery StartFresh() => ConceptMastery.Start(Guid.CreateVersion7(), Guid.CreateVersion7());

    [Fact]
    public void Start_BeginsAtZeroMastery_NotProvisional()
    {
        var mastery = StartFresh();

        mastery.Mastery.Should().Be(0);
        mastery.Provisional.Should().BeFalse();
        mastery.Attempts.Should().Be(0);
    }

    [Fact]
    public void SeedFromPlacement_IsProvisionalUntilObserved()
    {
        var mastery = ConceptMastery.SeedFromPlacement(Guid.CreateVersion7(), Guid.CreateVersion7(), 0.7);

        mastery.Provisional.Should().BeTrue();

        mastery.ApplyObservation(outcome: 1.0, hintsUsed: 0, guess: 0.2, slip: 0.1, Now);

        mastery.Provisional.Should().BeFalse();
    }

    [Fact]
    public void ApplyObservation_UpdatesCountsAndRaisesEvent()
    {
        var mastery = StartFresh();

        mastery.ApplyObservation(outcome: 1.0, hintsUsed: 0, guess: 0.2, slip: 0.1, Now);

        mastery.Attempts.Should().Be(1);
        mastery.Correct.Should().Be(1);
        mastery.LastPracticedAt.Should().Be(Now);
        mastery.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ConceptMasteryChanged>();
    }

    [Fact]
    public void ThreeWrongInARow_TriggersRemediation()
    {
        var mastery = StartFresh();

        for (var i = 0; i < ConceptMastery.RemediationStreak; i++)
        {
            mastery.NeedsRemediation.Should().BeFalse();
            mastery.ApplyObservation(outcome: 0.0, hintsUsed: 0, guess: 0.2, slip: 0.1, Now);
        }

        mastery.NeedsRemediation.Should().BeTrue();
    }

    [Fact]
    public void CorrectAnswer_ResetsWrongStreak()
    {
        var mastery = StartFresh();
        mastery.ApplyObservation(outcome: 0.0, hintsUsed: 0, guess: 0.2, slip: 0.1, Now);
        mastery.ApplyObservation(outcome: 0.0, hintsUsed: 0, guess: 0.2, slip: 0.1, Now);

        mastery.ApplyObservation(outcome: 1.0, hintsUsed: 0, guess: 0.2, slip: 0.1, Now);

        mastery.StreakWrong.Should().Be(0);
    }

    [Fact]
    public void CutForRemediation_CapsMasteryAndClearsStreak()
    {
        var mastery = ConceptMastery.SeedFromPlacement(Guid.CreateVersion7(), Guid.CreateVersion7(), 0.9);

        mastery.CutForRemediation(Now);

        mastery.Mastery.Should().BeApproximately(0.6, 1e-12);
        mastery.StreakWrong.Should().Be(0);
        mastery.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ConceptMasteryChanged>();
    }

    [Fact]
    public void CutForRemediation_BelowCap_DoesNotRaiseOrRaiseMastery()
    {
        var mastery = ConceptMastery.SeedFromPlacement(Guid.CreateVersion7(), Guid.CreateVersion7(), 0.4);

        mastery.CutForRemediation(Now);

        mastery.Mastery.Should().BeApproximately(0.4, 1e-12);
        mastery.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Confidence_SaturatesWithAttempts()
    {
        var mastery = StartFresh();

        mastery.ApplyObservation(outcome: 1.0, hintsUsed: 0, guess: 0.2, slip: 0.1, Now);
        var afterOne = mastery.Confidence;

        for (var i = 0; i < 19; i++)
        {
            mastery.ApplyObservation(outcome: 1.0, hintsUsed: 0, guess: 0.2, slip: 0.1, Now);
        }

        mastery.Confidence.Should().BeGreaterThan(afterOne).And.BeLessThan(1.0);
    }
}
