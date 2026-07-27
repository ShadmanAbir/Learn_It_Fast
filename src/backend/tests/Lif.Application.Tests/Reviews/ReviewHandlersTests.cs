using Lif.Application.Reviews;
using Lif.Domain.Learner;
using Lif.Domain.Scheduling;

namespace Lif.Application.Tests.Reviews;

public class ReviewHandlersTests
{
    private static readonly Guid LearnerId = Guid.CreateVersion7();

    [Fact]
    public async Task Queue_ReturnsOnlyDueItems_OrderedByPriority()
    {
        var world = new TestWorld();
        var veryOverdue = ReviewItem.Introduce(
            LearnerId, ReviewItemKind.ConceptDrill, world.Greet.Id, TestWorld.Start.AddDays(-30));
        var slightlyOverdue = ReviewItem.Introduce(
            LearnerId, ReviewItemKind.ConceptDrill, world.Numbers.Id, TestWorld.Start.AddDays(-3));
        var notDue = ReviewItem.Introduce(
            LearnerId, ReviewItemKind.Word, Guid.CreateVersion7(), TestWorld.Start);
        world.Reviews.Items.AddRange([veryOverdue, slightlyOverdue, notDue]);

        var result = await world.GetReviewQueue.HandleAsync(new ReviewQueueQuery(LearnerId));

        result.IsSuccess.Should().BeTrue();
        result.Value.DueCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Select(i => i.ReviewItemId).Should().NotContain(notDue.Id);
        result.Value.Items.Should().BeInDescendingOrder(i => i.Priority);
        result.Value.Items[0].ReviewItemId.Should().Be(veryOverdue.Id); // most overdue wins
        veryOverdue.Priority.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Queue_HonorsLimit_WhileReportingFullDueCount()
    {
        var world = new TestWorld();
        world.Reviews.Items.AddRange([
            ReviewItem.Introduce(LearnerId, ReviewItemKind.ConceptDrill, world.Greet.Id, TestWorld.Start.AddDays(-10)),
            ReviewItem.Introduce(LearnerId, ReviewItemKind.ConceptDrill, world.Numbers.Id, TestWorld.Start.AddDays(-10)),
        ]);

        var result = await world.GetReviewQueue.HandleAsync(new ReviewQueueQuery(LearnerId, Limit: 1));

        result.Value.Items.Should().ContainSingle();
        result.Value.DueCount.Should().Be(2);
    }

    [Fact]
    public async Task Answer_ReschedulesItem_AndAppendsLog()
    {
        var world = new TestWorld();
        var item = ReviewItem.Introduce(
            LearnerId, ReviewItemKind.ConceptDrill, world.Greet.Id, TestWorld.Start.AddDays(-10));
        world.Reviews.Items.Add(item);

        var result = await world.AnswerReview.HandleAsync(
            new AnswerReviewCommand(LearnerId, item.Id, Rating: 3));

        result.IsSuccess.Should().BeTrue();
        result.Value.NextDueAt.Should().BeAfter(TestWorld.Start);
        result.Value.Stability.Should().BeGreaterThan(0);
        result.Value.WordStatus.Should().BeNull(); // concept drill, no word state
        world.Reviews.Logs.Should().ContainSingle(l => l.ReviewItemId == item.Id && l.Rating == ReviewRating.Good);
    }

    [Fact]
    public async Task Answer_WordItem_DrivesWordStatusMachine()
    {
        var world = new TestWorld();
        var vocabId = Guid.CreateVersion7();
        var item = ReviewItem.Introduce(LearnerId, ReviewItemKind.Word, vocabId, TestWorld.Start.AddDays(-10));
        world.Reviews.Items.Add(item);

        var again = await world.AnswerReview.HandleAsync(
            new AnswerReviewCommand(LearnerId, item.Id, Rating: 1));

        again.IsSuccess.Should().BeTrue();
        again.Value.WordStatus.Should().Be("Learning"); // lapse from New → Learning
        var word = world.WordStates.Rows.Should().ContainSingle(w => w.VocabEntryId == vocabId).Subject;
        word.LapseCount.Should().Be(1);
    }

    [Fact]
    public async Task Answer_InvalidRating_IsRejected()
    {
        var world = new TestWorld();

        var result = await world.AnswerReview.HandleAsync(
            new AnswerReviewCommand(LearnerId, Guid.CreateVersion7(), Rating: 5));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("review.invalid_rating");
    }

    [Fact]
    public async Task Answer_OtherLearnersItem_ReturnsNotFound()
    {
        var world = new TestWorld();
        var item = ReviewItem.Introduce(
            LearnerId, ReviewItemKind.ConceptDrill, world.Greet.Id, TestWorld.Start.AddDays(-10));
        world.Reviews.Items.Add(item);

        var result = await world.AnswerReview.HandleAsync(
            new AnswerReviewCommand(Guid.CreateVersion7(), item.Id, Rating: 3));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("review.item_not_found");
    }
}
