using Lif.Application.Attempts;
using Lif.Application.Reviews;
using Lif.Application.Sessions;
using Lif.Application.Tests;
using Lif.SharedKernel.Results;

namespace Lif.Simulation.Tests;

/// <summary>What one virtual day looked like, for invariant assertions.</summary>
public sealed record DayStats(
    int Day,
    bool Skipped,
    int DueAtMorning,
    int ReviewsAnswered,
    int ExercisesAttempted,
    int CorrectAttempts);

/// <summary>Outcome of a full journey: the world stays inspectable for pedagogy assertions.</summary>
public sealed class JourneyResult(TestWorld world, Guid learnerId, IReadOnlyList<DayStats> days)
{
    public TestWorld World { get; } = world;

    public Guid LearnerId { get; } = learnerId;

    public IReadOnlyList<DayStats> Days { get; } = days;

    public double MasteryOf(Guid conceptId) =>
        World.Masteries.Rows.SingleOrDefault(m => m.LearnerId == LearnerId && m.ConceptId == conceptId)?.Mastery ?? 0.0;

    public int MaxDueBacklog => Days.Count == 0 ? 0 : Days.Max(d => d.DueAtMorning);

    public int TotalAttempts => Days.Sum(d => d.ExercisesAttempted);
}

/// <summary>
/// Simulation harness v1 (doc 17 §2, NFR-TST-2): one virtual student runs a daily
/// reviews-then-lesson loop against the real planner/scheduler/BKT/FSRS stack via the
/// Application handlers, with the clock advanced one day at a time. Any handler failure
/// outside the expected flow aborts the journey — a journey that cannot run IS the regression.
/// </summary>
public static class JourneyRunner
{
    public static async Task<JourneyResult> RunAsync(StudentProfile profile, int days)
    {
        var world = new TestWorld();
        var student = new SimulatedStudent(profile);
        var learnerId = await world.OnboardAsync(profile.MinutesPerDay);
        var stats = new List<DayStats>();

        for (var day = 1; day <= days; day++)
        {
            var dueAtMorning = await world.Reviews.CountDueAsync(learnerId, world.Clock.UtcNow);

            if (student.SkipsToday())
            {
                stats.Add(new DayStats(day, Skipped: true, dueAtMorning, 0, 0, 0));
                world.Clock.Advance(TimeSpan.FromDays(1));
                continue;
            }

            var reviewsAnswered = await ClearReviewQueueAsync(world, student, learnerId);
            var (attempted, correct) = await RunSessionAsync(world, student, learnerId);

            stats.Add(new DayStats(day, Skipped: false, dueAtMorning, reviewsAnswered, attempted, correct));
            world.Clock.Advance(TimeSpan.FromDays(1));
        }

        return new JourneyResult(world, learnerId, stats);
    }

    /// <summary>Answers every due card once, in the prioritizer's order.</summary>
    private static async Task<int> ClearReviewQueueAsync(TestWorld world, SimulatedStudent student, Guid learnerId)
    {
        var queue = Expect(await world.GetReviewQueue.HandleAsync(new ReviewQueueQuery(learnerId, Limit: 100)));
        foreach (var item in queue.Items)
        {
            Expect(await world.AnswerReview.HandleAsync(
                new AnswerReviewCommand(learnerId, item.ReviewItemId, student.RateReview())));
        }

        return queue.Items.Count;
    }

    /// <summary>Plans, starts, answers every materialized exercise and completes the session.</summary>
    private static async Task<(int Attempted, int Correct)> RunSessionAsync(
        TestWorld world, SimulatedStudent student, Guid learnerId)
    {
        var planned = Expect(await world.PlanSession.HandleAsync(new PlanSessionCommand(learnerId)));
        var session = world.Sessions.Sessions.Single(s => s.Id == planned.SessionId);
        Expect(await world.SessionLifecycle.StartAsync(new SessionCommand(learnerId, session.Id)));

        var attempted = 0;
        var correct = 0;
        foreach (var activity in session.Activities)
        {
            foreach (var exercise in activity.Exercises)
            {
                var attempt = Expect(await world.RecordAttempt.HandleAsync(new RecordAttemptCommand(
                    learnerId, exercise.Id, student.AnswerFor(exercise.Spec))));
                attempted++;
                if (attempt.Outcome >= 0.5)
                {
                    correct++;
                }
            }

            Expect(await world.SessionLifecycle.CompleteActivityAsync(
                new CompleteActivityCommand(learnerId, session.Id, activity.Id)));
        }

        Expect(await world.SessionLifecycle.CompleteAsync(new SessionCommand(learnerId, session.Id)));
        return (attempted, correct);
    }

    private static T Expect<T>(Result<T> result)
    {
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Journey aborted: {result.Error.Code} — {result.Error.Message}");
        }

        return result.Value;
    }
}
