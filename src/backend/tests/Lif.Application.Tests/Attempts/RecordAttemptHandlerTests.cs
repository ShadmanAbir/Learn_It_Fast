using Lif.Application.Attempts;
using Lif.Application.Sessions;
using Lif.Domain.Scheduling;

namespace Lif.Application.Tests.Attempts;

public class RecordAttemptHandlerTests
{
    private const string MorningStem = "Wie sagt man 'good morning'?";

    private static async Task<(TestWorld World, Guid LearnerId, ExerciseDto Exercise)> StartedSessionAsync()
    {
        var world = new TestWorld();
        var learnerId = await world.OnboardAsync();
        var planned = await world.PlanSession.HandleAsync(new PlanSessionCommand(learnerId));
        var started = await world.SessionLifecycle.StartAsync(new SessionCommand(learnerId, planned.Value.SessionId));
        var exercise = started.Value.Activities
            .SelectMany(a => a.Exercises)
            .Single(e => e.Stem == MorningStem);
        return (world, learnerId, exercise);
    }

    [Fact]
    public async Task Handle_CorrectAnswer_UpdatesMastery_AndSchedulesReview()
    {
        var (world, learnerId, exercise) = await StartedSessionAsync();

        var result = await world.RecordAttempt.HandleAsync(
            new RecordAttemptCommand(learnerId, exercise.ExerciseInstanceId, "Guten Morgen"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be(1.0);
        result.Value.EvaluatedBy.Should().Be("deterministic");
        result.Value.CorrectAnswer.Should().Be("Guten Morgen");

        var delta = result.Value.MasteryDeltas.Should().ContainSingle().Subject;
        delta.ConceptId.Should().Be(world.Greet.Id);
        delta.To.Should().BeGreaterThan(delta.From);
        delta.NeedsRemediation.Should().BeFalse();

        world.Masteries.Rows.Should().ContainSingle(m => m.ConceptId == world.Greet.Id && m.Attempts == 1);
        world.Reviews.Items.Should().ContainSingle(i =>
            i.Kind == ReviewItemKind.ConceptDrill && i.ItemRef == world.Greet.Id);
        world.Mistakes.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WrongAnswer_LogsMistakeWithTaxonomyCode()
    {
        var (world, learnerId, exercise) = await StartedSessionAsync();

        var result = await world.RecordAttempt.HandleAsync(
            new RecordAttemptCommand(learnerId, exercise.ExerciseInstanceId, "Gute Nacht"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Outcome.Should().Be(0.0);

        var mistake = world.Mistakes.Rows.Should().ContainSingle().Subject;
        mistake.ErrorCode.Should().Be("GREET_TIME");
        mistake.ConceptId.Should().Be(world.Greet.Id);
        mistake.Wrong.Should().Be("Gute Nacht");
        mistake.Right.Should().Be("Guten Morgen");
    }

    [Fact]
    public async Task Handle_SecondAttemptOnSameConcept_DoesNotDuplicateReviewItem()
    {
        var (world, learnerId, exercise) = await StartedSessionAsync();
        await world.RecordAttempt.HandleAsync(
            new RecordAttemptCommand(learnerId, exercise.ExerciseInstanceId, "Guten Morgen"));

        await world.RecordAttempt.HandleAsync(
            new RecordAttemptCommand(learnerId, exercise.ExerciseInstanceId, "Guten Morgen"));

        world.Reviews.Items.Should().ContainSingle(i => i.ItemRef == world.Greet.Id);
        world.Masteries.Rows.Single(m => m.ConceptId == world.Greet.Id).Attempts.Should().Be(2);
    }

    [Fact]
    public async Task Handle_UnknownExercise_ReturnsNotFound()
    {
        var (world, learnerId, _) = await StartedSessionAsync();

        var result = await world.RecordAttempt.HandleAsync(
            new RecordAttemptCommand(learnerId, Guid.CreateVersion7(), "egal"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("exercise.not_found");
    }

    [Fact]
    public async Task Handle_OtherLearnersExercise_ReturnsNotFound()
    {
        var (world, _, exercise) = await StartedSessionAsync();

        var result = await world.RecordAttempt.HandleAsync(
            new RecordAttemptCommand(Guid.CreateVersion7(), exercise.ExerciseInstanceId, "Guten Morgen"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("exercise.not_found");
    }

    [Fact]
    public async Task Handle_PlannedButNotStartedSession_Fails()
    {
        var world = new TestWorld();
        var learnerId = await world.OnboardAsync();
        var planned = await world.PlanSession.HandleAsync(new PlanSessionCommand(learnerId));
        var exercise = planned.Value.Activities.SelectMany(a => a.Exercises).First();

        var result = await world.RecordAttempt.HandleAsync(
            new RecordAttemptCommand(learnerId, exercise.ExerciseInstanceId, "Guten Morgen"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("learning.session_not_active");
    }
}
