using Lif.Application.Learners;
using Lif.Application.Today;
using Microsoft.AspNetCore.Mvc;

namespace Lif.Api.Endpoints;

/// <summary>Request body for PUT /me/goals; the learner id comes from the identity header.</summary>
public sealed record UpdateGoalsRequest(string TargetLevel, int MinutesPerDay);

/// <summary>Onboarding fast path + profile + Today bundle (doc 12 §2 Auth &amp; Profile, doc 09).</summary>
public static class LearnerEndpoints
{
    public static RouteGroupBuilder MapLearnerEndpoints(this RouteGroupBuilder api)
    {
        // Handlers are [FromServices]: without the attribute, minimal API inference probes DI at
        // endpoint-build time and hosts without a database (health-only boot) would fail to route.
        // Beginner fast path: creates the learner; the id doubles as the device identity (Phase 1).
        api.MapPost("/onboarding", async (
            OnboardLearnerCommand command, [FromServices] OnboardLearnerHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/me", result.Value)
                : ApiResults.Problem(result.Error);
        });

        api.MapGet("/me", async (
            [FromHeader(Name = ApiResults.LearnerHeader)] Guid? learnerId,
            [FromServices] GetLearnerHandler handler, CancellationToken ct) =>
        {
            if (learnerId is null)
            {
                return ApiResults.LearnerRequired();
            }

            var result = await handler.HandleAsync(learnerId.Value, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result.Error);
        });

        api.MapPut("/me/goals", async (
            [FromHeader(Name = ApiResults.LearnerHeader)] Guid? learnerId,
            UpdateGoalsRequest request, [FromServices] UpdateGoalsHandler handler, CancellationToken ct) =>
        {
            if (learnerId is null)
            {
                return ApiResults.LearnerRequired();
            }

            var result = await handler.HandleAsync(
                new UpdateGoalsCommand(learnerId.Value, request.TargetLevel, request.MinutesPerDay), ct);
            return result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result.Error);
        });

        api.MapGet("/today", async (
            [FromHeader(Name = ApiResults.LearnerHeader)] Guid? learnerId,
            [FromServices] GetTodayHandler handler, CancellationToken ct) =>
        {
            if (learnerId is null)
            {
                return ApiResults.LearnerRequired();
            }

            var result = await handler.HandleAsync(learnerId.Value, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result.Error);
        });

        return api;
    }
}
