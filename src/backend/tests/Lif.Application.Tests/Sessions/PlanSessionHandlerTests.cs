using Lif.Application.Sessions;
using Lif.Domain.Scheduling;

namespace Lif.Application.Tests.Sessions;

public class PlanSessionHandlerTests
{
    [Fact]
    public async Task Handle_PlansLessonFromFrontier_WithCuratedExercises()
    {
        var world = new TestWorld();
        var learnerId = await world.OnboardAsync(minutesPerDay: 15);

        var result = await world.PlanSession.HandleAsync(new PlanSessionCommand(learnerId));

        result.IsSuccess.Should().BeTrue();
        var session = result.Value;
        session.Status.Should().Be("Planned");
        session.TimeBudgetMinutes.Should().Be(15);
        session.PlannerVersion.Should().Be("1.0.0");
        session.Rationale.Should().NotBeEmpty();

        // No reviews due → a single lesson; wfragen is gated behind greet (mastery 0 < 0.8).
        session.Activities.Should().ContainSingle();
        var lesson = session.Activities[0];
        lesson.Kind.Should().Be("Lesson");
        lesson.Exercises.Should().HaveCount(3); // greet: 2 MC, numbers: 1 cloze
        lesson.Exercises.Should().OnlyContain(e => e.Source == "Curated");
        lesson.Exercises.Select(e => e.Stem).Should().Contain("zwei plus zwei ist ___");
        session.Rationale.Should().NotContain(r => r.Contains("W-Fragen", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Handle_AddsReviewActivity_WhenReviewsAreDue()
    {
        var world = new TestWorld();
        var learnerId = await world.OnboardAsync();
        world.Reviews.Add(ReviewItem.Introduce(
            learnerId, ReviewItemKind.ConceptDrill, world.Greet.Id, TestWorld.Start.AddDays(-10)));

        var result = await world.PlanSession.HandleAsync(new PlanSessionCommand(learnerId));

        result.IsSuccess.Should().BeTrue();
        var review = result.Value.Activities[0];
        review.Kind.Should().Be("Review");
        review.Exercises.Should().BeEmpty(); // served by the review queue endpoints
        result.Value.Rationale.Should().Contain(r => r.Contains("review", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_Conflicts_WhenSessionAlreadyOpen()
    {
        var world = new TestWorld();
        var learnerId = await world.OnboardAsync();
        (await world.PlanSession.HandleAsync(new PlanSessionCommand(learnerId))).IsSuccess.Should().BeTrue();

        var second = await world.PlanSession.HandleAsync(new PlanSessionCommand(learnerId));

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("session.already_open");
    }

    [Fact]
    public async Task Handle_UnknownLearner_ReturnsNotFound()
    {
        var world = new TestWorld();

        var result = await world.PlanSession.HandleAsync(new PlanSessionCommand(Guid.CreateVersion7()));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("learner.not_found");
    }

    [Fact]
    public async Task GetCurrent_ReturnsOpenSession_UntilAbandoned()
    {
        var world = new TestWorld();
        var learnerId = await world.OnboardAsync();
        var planned = await world.PlanSession.HandleAsync(new PlanSessionCommand(learnerId));

        var current = await world.GetCurrentSession.HandleAsync(learnerId);
        current.IsSuccess.Should().BeTrue();
        current.Value.SessionId.Should().Be(planned.Value.SessionId);

        (await world.SessionLifecycle.AbandonAsync(new SessionCommand(learnerId, planned.Value.SessionId)))
            .IsSuccess.Should().BeTrue();

        var afterAbandon = await world.GetCurrentSession.HandleAsync(learnerId);
        afterAbandon.IsFailure.Should().BeTrue();
        afterAbandon.Error.Code.Should().Be("session.not_found");
    }

    [Fact]
    public async Task Lifecycle_StartCompleteActivityComplete_TransitionsStatuses()
    {
        var world = new TestWorld();
        var learnerId = await world.OnboardAsync();
        var planned = await world.PlanSession.HandleAsync(new PlanSessionCommand(learnerId));
        var sessionId = planned.Value.SessionId;

        var started = await world.SessionLifecycle.StartAsync(new SessionCommand(learnerId, sessionId));
        started.IsSuccess.Should().BeTrue();
        started.Value.Status.Should().Be("Active");
        started.Value.Activities[0].Status.Should().Be("Active");

        var afterActivity = await world.SessionLifecycle.CompleteActivityAsync(
            new CompleteActivityCommand(learnerId, sessionId, started.Value.Activities[0].ActivityId));
        afterActivity.IsSuccess.Should().BeTrue();
        afterActivity.Value.Activities[0].Status.Should().Be("Completed");

        var completed = await world.SessionLifecycle.CompleteAsync(new SessionCommand(learnerId, sessionId));
        completed.IsSuccess.Should().BeTrue();
        completed.Value.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Lifecycle_OtherLearner_CannotTouchSession()
    {
        var world = new TestWorld();
        var learnerId = await world.OnboardAsync();
        var planned = await world.PlanSession.HandleAsync(new PlanSessionCommand(learnerId));

        var result = await world.SessionLifecycle.StartAsync(
            new SessionCommand(Guid.CreateVersion7(), planned.Value.SessionId));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("session.not_found");
    }
}
