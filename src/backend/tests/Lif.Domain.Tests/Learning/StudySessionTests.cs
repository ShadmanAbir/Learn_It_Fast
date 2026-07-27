using Lif.Domain.Learning;
using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Tests.Learning;

public class StudySessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid LearnerId = Guid.CreateVersion7();

    private static SessionPlan TwoActivityPlan() =>
        new(
            ReviewShare: 0.4,
            NewShare: 0.6,
            FreeShare: 0.0,
            Activities:
            [
                new ActivityPlan(ActivityKind.Review, 4, [], 8),
                new ActivityPlan(ActivityKind.Lesson, 6, [Guid.CreateVersion7()], 1),
            ],
            Rationale: ["8 reviews due"],
            Seed: 42,
            PlannerVersion: SessionPlanner.Version);

    private static ExerciseSpec ClozeSpec() =>
        new(ExerciseType.Cloze, "Das ist ___ Tisch.", [], "ein", [Guid.CreateVersion7()], Difficulty.From(2));

    private static StudySession ActiveSessionWithExercise(out ExerciseInstance exercise)
    {
        var session = StudySession.FromPlan(LearnerId, 10, TwoActivityPlan());
        exercise = session.Activities[1].AddCuratedExercise(ClozeSpec());
        session.Start(Now).IsSuccess.Should().BeTrue();
        return session;
    }

    [Fact]
    public void FromPlan_MaterializesActivitiesInPlanOrder()
    {
        var session = StudySession.FromPlan(LearnerId, 10, TwoActivityPlan());

        session.Status.Should().Be(SessionStatus.Planned);
        session.Activities.Should().HaveCount(2);
        session.Activities.Select(a => a.Kind).Should().Equal(ActivityKind.Review, ActivityKind.Lesson);
        session.Activities.Select(a => a.OrderIndex).Should().Equal(0, 1);
        session.Activities.Should().OnlyContain(a => a.Status == ActivityStatus.Pending);
    }

    [Fact]
    public void Start_ActivatesSessionAndFirstActivity()
    {
        var session = StudySession.FromPlan(LearnerId, 10, TwoActivityPlan());

        session.Start(Now).IsSuccess.Should().BeTrue();

        session.Status.Should().Be(SessionStatus.Active);
        session.StartedAt.Should().Be(Now);
        session.Activities[0].Status.Should().Be(ActivityStatus.Active);
        session.Activities[1].Status.Should().Be(ActivityStatus.Pending);
    }

    [Fact]
    public void Start_Twice_Fails()
    {
        var session = StudySession.FromPlan(LearnerId, 10, TwoActivityPlan());
        session.Start(Now);

        session.Start(Now.AddMinutes(1)).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecordAttempt_BeforeStart_Fails()
    {
        var session = StudySession.FromPlan(LearnerId, 10, TwoActivityPlan());
        var exercise = session.Activities[1].AddCuratedExercise(ClozeSpec());

        session.RecordAttempt(exercise.Id, "ein", 0, null, Now).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecordAttempt_EvaluatesAndRaisesEvent()
    {
        var session = ActiveSessionWithExercise(out var exercise);

        var result = session.RecordAttempt(exercise.Id, "ein", hintsUsed: 1, latencyMs: 2500, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be(1.0);
        result.Value.HintsUsed.Should().Be(1);
        exercise.Attempts.Should().ContainSingle();

        var raised = session.DomainEvents.OfType<AttemptRecorded>().Single();
        raised.Outcome.Should().Be(1.0);
        raised.ExerciseInstanceId.Should().Be(exercise.Id);
        raised.LearnerId.Should().Be(LearnerId);
    }

    [Fact]
    public void RecordAttempt_ForUnknownExercise_Fails()
    {
        var session = ActiveSessionWithExercise(out _);

        session.RecordAttempt(Guid.CreateVersion7(), "ein", 0, null, Now).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CompleteActivity_AdvancesToTheNextPendingActivity()
    {
        var session = ActiveSessionWithExercise(out _);

        session.CompleteActivity(session.Activities[0].Id).IsSuccess.Should().BeTrue();

        session.Activities[0].Status.Should().Be(ActivityStatus.Completed);
        session.Activities[1].Status.Should().Be(ActivityStatus.Active);
    }

    [Fact]
    public void Complete_SkipsUnfinishedActivities_AndSummarizesAttempts()
    {
        var session = ActiveSessionWithExercise(out var exercise);
        session.RecordAttempt(exercise.Id, "kein", 0, null, Now); // wrong → 0.0

        session.Complete(Now.AddMinutes(10)).IsSuccess.Should().BeTrue();

        session.Status.Should().Be(SessionStatus.Completed);
        session.CompletedAt.Should().Be(Now.AddMinutes(10));
        session.Activities.Should().OnlyContain(a => a.Status == ActivityStatus.Skipped);

        var summary = session.DomainEvents.OfType<SessionCompleted>().Single();
        summary.AttemptCount.Should().Be(1);
        summary.AverageOutcome.Should().Be(0.0);
    }

    [Fact]
    public void Complete_AfterAbandon_Fails()
    {
        var session = ActiveSessionWithExercise(out _);
        session.Abandon(Now).IsSuccess.Should().BeTrue();

        session.Status.Should().Be(SessionStatus.Abandoned);
        session.Complete(Now.AddMinutes(1)).IsFailure.Should().BeTrue();
        session.Abandon(Now.AddMinutes(1)).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void PlannedSession_CanBeAbandoned()
    {
        var session = StudySession.FromPlan(LearnerId, 10, TwoActivityPlan());

        session.Abandon(Now).IsSuccess.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Abandoned);
    }
}
