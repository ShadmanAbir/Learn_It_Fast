using Lif.Domain.Learning;

namespace Lif.Simulation.Tests;

/// <summary>
/// 14-day virtual journeys (doc 17 §2 smoke tier): pedagogy invariants over the real
/// planner/scheduler/BKT/FSRS stack. Seeds are fixed, so every assertion is deterministic.
/// </summary>
public class VirtualJourneyTests
{
    private const int Days = 14;

    [Fact]
    public async Task DiligentStudent_MastersTheSeedSlice_WithoutReviewDebt()
    {
        var result = await JourneyRunner.RunAsync(StudentProfile.Diligent(), Days);
        var world = result.World;

        // Every planned session was driven to completion — the loop never wedged.
        world.Sessions.Sessions.Should().NotBeEmpty();
        world.Sessions.Sessions.Should().OnlyContain(s => s.Status == SessionStatus.Completed);

        // Mastery grows: greetings crossed the 0.8 threshold, which unlocked W-Fragen (requires-edge).
        result.MasteryOf(world.Greet.Id).Should().BeGreaterThanOrEqualTo(0.8);
        result.MasteryOf(world.WFragen.Id).Should().BeGreaterThan(0.0, "mastering greet must unlock the frontier successor");
        result.MasteryOf(world.Numbers.Id).Should().BeGreaterThanOrEqualTo(0.8);

        // Each practiced concept entered the review queue exactly once (doc 08 §7).
        world.Reviews.Items.Should().HaveCount(3);
        world.Reviews.Items.Select(i => i.ItemRef).Should().OnlyHaveUniqueItems();

        // Review debt stays bounded for a daily-active profile (NFR-TST-2 invariant).
        result.MaxDueBacklog.Should().BeLessThanOrEqualTo(world.Reviews.Items.Count);
    }

    [Fact]
    public async Task StrugglingStudent_ProgressesSlower_ButInvariantsHold()
    {
        var diligent = await JourneyRunner.RunAsync(StudentProfile.Diligent(), Days);
        var struggling = await JourneyRunner.RunAsync(StudentProfile.Struggling(), Days);

        struggling.MasteryOf(struggling.World.Greet.Id)
            .Should().BeLessThan(diligent.MasteryOf(diligent.World.Greet.Id));

        // Wrong answers are never silently dropped: the mistake log grew (FR-EXR-3).
        struggling.World.Mistakes.Rows.Should().NotBeEmpty();

        // Skipped days leave no dangling open session.
        struggling.World.Sessions.Sessions
            .Should().OnlyContain(s => s.Status == SessionStatus.Completed);
    }

    [Fact]
    public async Task SameSeed_ReproducesTheJourneyExactly()
    {
        var first = await JourneyRunner.RunAsync(StudentProfile.Struggling(seed: 7), Days);
        var second = await JourneyRunner.RunAsync(StudentProfile.Struggling(seed: 7), Days);

        second.TotalAttempts.Should().Be(first.TotalAttempts);
        second.Days.Should().BeEquivalentTo(first.Days, options => options.WithStrictOrdering());
        second.MasteryOf(second.World.Greet.Id).Should().Be(first.MasteryOf(first.World.Greet.Id));
    }

    [Fact]
    public async Task SmokeCohort_AllProfilesFinishWithBoundedStateAndDebt()
    {
        StudentProfile[] cohort =
        [
            StudentProfile.Diligent(seed: 11),
            StudentProfile.Diligent(seed: 12),
            StudentProfile.Struggling(seed: 13),
            StudentProfile.Struggling(seed: 14),
            new("guesser", Accuracy: 0.25, SkipProbability: 0.4, Seed: 15),
        ];

        foreach (var profile in cohort)
        {
            var result = await JourneyRunner.RunAsync(profile, Days);

            result.World.Masteries.Rows
                .Should().OnlyContain(m => m.Mastery >= 0.0 && m.Mastery <= 1.0, profile.Name);
            result.World.Sessions.Sessions
                .Should().OnlyContain(s => s.Status == SessionStatus.Completed, profile.Name);
            result.MaxDueBacklog.Should().BeLessThanOrEqualTo(10, profile.Name);
        }
    }
}
