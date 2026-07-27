using Lif.Application.Learners;

namespace Lif.Application.Tests.Learners;

public class OnboardLearnerHandlerTests
{
    [Fact]
    public async Task Handle_CreatesBeginnerAtA1_1_AgainstActiveCurriculum()
    {
        var world = new TestWorld();

        var result = await world.OnboardLearner.HandleAsync(
            new OnboardLearnerCommand("  Alex  ", "B1", 300, "career"));

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrentLevel.Should().Be("A1.1");
        result.Value.TargetLevel.Should().Be("B1.1");
        result.Value.MinutesPerDay.Should().Be(240); // clamped to the 5–240 range
        result.Value.CurriculumVersionId.Should().Be(world.Version.Id);
        world.Learners.Profiles.Should().ContainSingle(p => p.DisplayName == "Alex");
        world.UnitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_RejectsInvalidTargetLevel()
    {
        var world = new TestWorld();

        var result = await world.OnboardLearner.HandleAsync(
            new OnboardLearnerCommand("Alex", "Z9", 15));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("learner.invalid_target_level");
    }

    [Fact]
    public async Task Handle_FailsWithoutPublishedCurriculum()
    {
        var world = new TestWorld();
        world.Curriculum.ActiveVersion = null;

        var result = await world.OnboardLearner.HandleAsync(
            new OnboardLearnerCommand("Alex", "B1", 15));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("curriculum.not_available");
    }

    [Fact]
    public async Task GetLearner_ReturnsProfile_AndUpdateGoals_Persists()
    {
        var world = new TestWorld();
        var learnerId = await world.OnboardAsync(minutesPerDay: 15);

        var update = await world.UpdateGoals.HandleAsync(new UpdateGoalsCommand(learnerId, "A2.2", 30));
        update.IsSuccess.Should().BeTrue();

        var me = await world.GetLearner.HandleAsync(learnerId);
        me.IsSuccess.Should().BeTrue();
        me.Value.TargetLevel.Should().Be("A2.2");
        me.Value.MinutesPerDay.Should().Be(30);
    }

    [Fact]
    public async Task GetLearner_UnknownLearner_ReturnsNotFound()
    {
        var world = new TestWorld();

        var result = await world.GetLearner.HandleAsync(Guid.CreateVersion7());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("learner.not_found");
    }
}
