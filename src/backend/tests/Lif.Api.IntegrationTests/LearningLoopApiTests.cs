using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lif.Application.Attempts;
using Lif.Application.Learners;
using Lif.Application.Reviews;
using Lif.Application.Sessions;
using Lif.Application.Today;
using Lif.Application.Tests;
using Lif.Domain.Scheduling;

namespace Lif.Api.IntegrationTests;

/// <summary>Walks the Phase-1 learner loop over real HTTP: onboarding → plan → attempt → reviews (doc 12 §2).</summary>
public class LearningLoopApiTests
{
    private const string LearnerHeader = "X-Learner-Id";
    private const string MorningStem = "Wie sagt man 'good morning'?";

    private static async Task<Guid> OnboardAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/onboarding",
            new { displayName = "Alex", targetLevel = "B1", minutesPerDay = 15 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<OnboardLearnerResult>();
        client.DefaultRequestHeaders.Add(LearnerHeader, result!.LearnerId.ToString());
        return result.LearnerId;
    }

    [Fact]
    public async Task Onboarding_CreatesLearner_AndMeReturnsProfile()
    {
        using var factory = new FakeBackedApiFactory();
        using var client = factory.CreateClient();
        var learnerId = await OnboardAsync(client);

        var me = await client.GetFromJsonAsync<LearnerDto>("/api/v1/me");

        me!.LearnerId.Should().Be(learnerId);
        me.DisplayName.Should().Be("Alex");
        me.CurrentLevel.Should().Be("A1.1");
        me.TargetLevel.Should().Be("B1.1");
        me.CurriculumVersionId.Should().Be(factory.World.Version.Id);
    }

    [Fact]
    public async Task MissingLearnerHeader_Returns401Problem()
    {
        using var factory = new FakeBackedApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("auth.learner_required");
    }

    [Fact]
    public async Task UnknownLearner_Returns404Problem_WithStableErrorCode()
    {
        using var factory = new FakeBackedApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(LearnerHeader, Guid.CreateVersion7().ToString());

        var response = await client.GetAsync(new Uri("/api/v1/me", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("learner.not_found");
    }

    [Fact]
    public async Task FullLoop_PlanStartAttemptCompleteSession()
    {
        using var factory = new FakeBackedApiFactory();
        using var client = factory.CreateClient();
        await OnboardAsync(client);

        // Plan
        var planResponse = await client.PostAsJsonAsync("/api/v1/sessions", new { });
        planResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var planned = await planResponse.Content.ReadFromJsonAsync<SessionDto>();
        planResponse.Headers.Location!.ToString().Should().Contain(planned!.SessionId.ToString());
        planned.Status.Should().Be("Planned");

        // Start
        var startResponse = await client.PostAsync(
            new Uri($"/api/v1/sessions/{planned.SessionId}/start", UriKind.Relative), content: null);
        var started = await startResponse.Content.ReadFromJsonAsync<SessionDto>();
        started!.Status.Should().Be("Active");

        // Attempt the greeting exercise deterministically
        var exercise = started.Activities.SelectMany(a => a.Exercises).Single(e => e.Stem == MorningStem);
        var attemptResponse = await client.PostAsJsonAsync(
            $"/api/v1/exercises/{exercise.ExerciseInstanceId}/attempts", new { response = "Guten Morgen" });
        attemptResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var attempt = await attemptResponse.Content.ReadFromJsonAsync<AttemptResultDto>();
        attempt!.Outcome.Should().Be(1.0);
        attempt.EvaluatedBy.Should().Be("deterministic");
        attempt.MasteryDeltas.Should().ContainSingle();

        // Complete activity + session
        var activityId = started.Activities[0].ActivityId;
        var afterActivity = await client.PostAsync(
            new Uri($"/api/v1/sessions/{planned.SessionId}/activities/{activityId}/complete", UriKind.Relative),
            content: null);
        (await afterActivity.Content.ReadFromJsonAsync<SessionDto>())!
            .Activities[0].Status.Should().Be("Completed");

        var completed = await client.PostAsync(
            new Uri($"/api/v1/sessions/{planned.SessionId}/complete", UriKind.Relative), content: null);
        (await completed.Content.ReadFromJsonAsync<SessionDto>())!.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task SecondOpenSession_Returns409Conflict()
    {
        using var factory = new FakeBackedApiFactory();
        using var client = factory.CreateClient();
        await OnboardAsync(client);
        (await client.PostAsJsonAsync("/api/v1/sessions", new { })).EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/v1/sessions", new { });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errorCode").GetString().Should().Be("session.already_open");
    }

    [Fact]
    public async Task ReviewQueue_ServesDueItem_AndAnswerReschedules()
    {
        using var factory = new FakeBackedApiFactory();
        using var client = factory.CreateClient();
        var learnerId = await OnboardAsync(client);
        var item = ReviewItem.Introduce(
            learnerId, ReviewItemKind.ConceptDrill, factory.World.Greet.Id, TestWorld.Start.AddDays(-10));
        factory.World.Reviews.Items.Add(item);

        var queue = await client.GetFromJsonAsync<ReviewQueueDto>("/api/v1/reviews/queue?limit=10");
        queue!.DueCount.Should().Be(1);
        queue.Items.Should().ContainSingle(i => i.ReviewItemId == item.Id);

        var answerResponse = await client.PostAsJsonAsync(
            $"/api/v1/reviews/{item.Id}/answers", new { rating = 3 });
        answerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var answer = await answerResponse.Content.ReadFromJsonAsync<AnswerReviewResult>();
        answer!.NextDueAt.Should().BeAfter(TestWorld.Start);
    }

    [Fact]
    public async Task Today_BundlesProfileDueCountAndOpenSession()
    {
        using var factory = new FakeBackedApiFactory();
        using var client = factory.CreateClient();
        var learnerId = await OnboardAsync(client);
        factory.World.Reviews.Items.Add(ReviewItem.Introduce(
            learnerId, ReviewItemKind.ConceptDrill, factory.World.Greet.Id, TestWorld.Start.AddDays(-10)));

        var before = await client.GetFromJsonAsync<TodayDto>("/api/v1/today");
        before!.Learner.LearnerId.Should().Be(learnerId);
        before.DueReviewCount.Should().Be(1);
        before.OpenSession.Should().BeNull();

        (await client.PostAsJsonAsync("/api/v1/sessions", new { })).EnsureSuccessStatusCode();

        var after = await client.GetFromJsonAsync<TodayDto>("/api/v1/today");
        after!.OpenSession.Should().NotBeNull();
        after.OpenSession!.Status.Should().Be("Planned");
    }
}
