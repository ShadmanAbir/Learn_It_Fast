using Lif.Application.Sessions;
using Lif.Application.Today;
using Lif.Domain.Scheduling;

namespace Lif.Application.Tests.Today;

public class GetTodayHandlerTests
{
    [Fact]
    public async Task Handle_BundlesProfile_DueCount_AndOpenSession()
    {
        var world = new TestWorld();
        var learnerId = await world.OnboardAsync();
        world.Reviews.Items.Add(ReviewItem.Introduce(
            learnerId, ReviewItemKind.ConceptDrill, world.Greet.Id, TestWorld.Start.AddDays(-10)));

        var before = await world.GetToday.HandleAsync(learnerId);

        before.IsSuccess.Should().BeTrue();
        before.Value.Learner.DisplayName.Should().Be("Alex");
        before.Value.DueReviewCount.Should().Be(1);
        before.Value.OpenSession.Should().BeNull();

        await world.PlanSession.HandleAsync(new PlanSessionCommand(learnerId));
        var after = await world.GetToday.HandleAsync(learnerId);

        after.Value.OpenSession.Should().NotBeNull();
        after.Value.OpenSession!.Status.Should().Be("Planned");
    }

    [Fact]
    public async Task Handle_UnknownLearner_ReturnsNotFound()
    {
        var world = new TestWorld();

        var result = await world.GetToday.HandleAsync(Guid.CreateVersion7());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("learner.not_found");
    }
}
