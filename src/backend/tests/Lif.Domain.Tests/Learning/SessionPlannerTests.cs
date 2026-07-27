using Lif.Domain.Learning;

namespace Lif.Domain.Tests.Learning;

public class SessionPlannerTests
{
    private static PlannerCandidate Candidate(string code, int minutes = 5, int unlocks = 0) =>
        new(Guid.CreateVersion7(), code, $"Title {code}", minutes, unlocks);

    private static SessionPlannerInput Input(
        int budget = 20,
        int dueReviews = 0,
        IReadOnlyList<PlannerCandidate>? candidates = null,
        int seed = 42) =>
        new(budget, dueReviews, candidates ?? [], seed);

    [Fact]
    public void ReviewsAreCappedAtFortyPercentOfBudget()
    {
        var plan = SessionPlanner.Plan(Input(budget: 20, dueReviews: 100, candidates: [Candidate("gram.a")]));

        var review = plan.Activities.Single(a => a.Kind == ActivityKind.Review);
        review.EstimatedMinutes.Should().Be(8); // floor(20 × 0.4)
        review.ItemCount.Should().Be(16); // 8 min × 2 items/min
        plan.Rationale.Should().Contain(r => r.Contains("priority-capped", StringComparison.Ordinal));
    }

    [Fact]
    public void SmallDueQueue_TakesOnlyTheTimeItNeeds()
    {
        var plan = SessionPlanner.Plan(Input(budget: 20, dueReviews: 4));

        var review = plan.Activities.Single(a => a.Kind == ActivityKind.Review);
        review.EstimatedMinutes.Should().Be(2); // ceil(4 / 2 per minute)
        review.ItemCount.Should().Be(4);
    }

    [Fact]
    public void NoReviewsDue_ExplainsAndPlansOnlyLessons()
    {
        var plan = SessionPlanner.Plan(Input(dueReviews: 0, candidates: [Candidate("gram.a")]));

        plan.Activities.Should().NotContain(a => a.Kind == ActivityKind.Review);
        plan.Rationale.Should().Contain(r => r.StartsWith("No reviews due", StringComparison.Ordinal));
    }

    [Fact]
    public void AtMostThreeNewConcepts_PerSession()
    {
        var candidates = Enumerable.Range(0, 6).Select(i => Candidate($"gram.{i}", minutes: 2)).ToList();

        var plan = SessionPlanner.Plan(Input(budget: 60, candidates: candidates));

        var lesson = plan.Activities.Single(a => a.Kind == ActivityKind.Lesson);
        lesson.ConceptIds.Should().HaveCount(SessionPlanner.MaxNewConcepts);
        lesson.ConceptIds.Should().Equal(candidates.Take(3).Select(c => c.ConceptId));
    }

    [Fact]
    public void TightBudget_StillTeachesAtLeastOneConcept()
    {
        var plan = SessionPlanner.Plan(Input(budget: 5, dueReviews: 10, candidates: [Candidate("gram.a", minutes: 15)]));

        var lesson = plan.Activities.Single(a => a.Kind == ActivityKind.Lesson);
        lesson.ConceptIds.Should().HaveCount(1);
    }

    [Fact]
    public void ConceptsBeyondTheBudget_AreCut()
    {
        var candidates = new[] { Candidate("gram.a", 8), Candidate("gram.b", 8), Candidate("gram.c", 8) };

        var plan = SessionPlanner.Plan(Input(budget: 18, candidates: candidates));

        // 18 min budget, no reviews: a (8) + b (16) fit, c (24) does not.
        plan.Activities.Single(a => a.Kind == ActivityKind.Lesson).ConceptIds.Should().HaveCount(2);
    }

    [Fact]
    public void EmptyFrontierAndNoReviews_ExplainsWhyThePlanIsEmpty()
    {
        var plan = SessionPlanner.Plan(Input(dueReviews: 0, candidates: []));

        plan.Activities.Should().BeEmpty();
        plan.Rationale.Should().Contain(r => r.Contains("Frontier is empty", StringComparison.Ordinal));
    }

    [Fact]
    public void EveryChosenConcept_GetsARationaleLine()
    {
        var candidates = new[] { Candidate("gram.a", unlocks: 2), Candidate("gram.b") };

        var plan = SessionPlanner.Plan(Input(budget: 30, candidates: candidates));

        plan.Rationale.Should().Contain(r => r.Contains("Title gram.a", StringComparison.Ordinal) && r.Contains("unlocks 2 more", StringComparison.Ordinal));
        plan.Rationale.Should().Contain(r => r.Contains("Title gram.b", StringComparison.Ordinal));
    }

    [Fact]
    public void SharesSumToOne_AndCarrySeedAndVersion()
    {
        var plan = SessionPlanner.Plan(Input(budget: 20, dueReviews: 10, candidates: [Candidate("gram.a")], seed: 1234));

        (plan.ReviewShare + plan.NewShare + plan.FreeShare).Should().BeApproximately(1.0, 0.011); // rounded to 2 dp
        plan.Seed.Should().Be(1234);
        plan.PlannerVersion.Should().Be(SessionPlanner.Version);
    }

    [Fact]
    public void SameInput_ProducesTheSamePlan()
    {
        var candidates = new[] { Candidate("gram.a"), Candidate("gram.b") };
        var input = Input(budget: 25, dueReviews: 7, candidates: candidates, seed: 99);

        var first = SessionPlanner.Plan(input);
        var second = SessionPlanner.Plan(input);

        // ActivityPlan holds a list, so compare structurally rather than by record equality.
        second.Activities.Should().BeEquivalentTo(first.Activities, o => o.WithStrictOrdering());
        second.Rationale.Should().Equal(first.Rationale);
        second.ReviewShare.Should().Be(first.ReviewShare);
    }
}
