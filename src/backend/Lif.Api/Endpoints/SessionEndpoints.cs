using Lif.Application.Attempts;
using Lif.Application.Sessions;
using Microsoft.AspNetCore.Mvc;

namespace Lif.Api.Endpoints;

/// <summary>Request body for POST /sessions.</summary>
public sealed record PlanSessionRequest(int? TimeBudgetMinutes = null);

/// <summary>Request body for POST /exercises/{id}/attempts (doc 12 §2).</summary>
public sealed record RecordAttemptRequest(string Response, int HintsUsed = 0, int? LatencyMs = null);

/// <summary>The core study loop: plan → start → attempts → complete (doc 12 §2 Study Sessions).</summary>
public static class SessionEndpoints
{
    public static RouteGroupBuilder MapSessionEndpoints(this RouteGroupBuilder api)
    {
        // Handlers are [FromServices] so hosts without a database (health-only boot) still route.
        api.MapPost("/sessions", async (
            [FromHeader(Name = ApiResults.LearnerHeader)] Guid? learnerId,
            PlanSessionRequest? request, [FromServices] PlanSessionHandler handler, CancellationToken ct) =>
        {
            if (learnerId is null)
            {
                return ApiResults.LearnerRequired();
            }

            var result = await handler.HandleAsync(
                new PlanSessionCommand(learnerId.Value, request?.TimeBudgetMinutes), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/sessions/{result.Value.SessionId}", result.Value)
                : ApiResults.Problem(result.Error);
        });

        api.MapGet("/sessions/current", async (
            [FromHeader(Name = ApiResults.LearnerHeader)] Guid? learnerId,
            [FromServices] GetCurrentSessionHandler handler, CancellationToken ct) =>
        {
            if (learnerId is null)
            {
                return ApiResults.LearnerRequired();
            }

            var result = await handler.HandleAsync(learnerId.Value, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result.Error);
        });

        api.MapPost("/sessions/{sessionId:guid}/start", async (
            [FromHeader(Name = ApiResults.LearnerHeader)] Guid? learnerId,
            Guid sessionId, [FromServices] SessionLifecycleHandler handler, CancellationToken ct) =>
        {
            if (learnerId is null)
            {
                return ApiResults.LearnerRequired();
            }

            var result = await handler.StartAsync(new SessionCommand(learnerId.Value, sessionId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result.Error);
        });

        api.MapPost("/sessions/{sessionId:guid}/activities/{activityId:guid}/complete", async (
            [FromHeader(Name = ApiResults.LearnerHeader)] Guid? learnerId,
            Guid sessionId, Guid activityId, [FromServices] SessionLifecycleHandler handler, CancellationToken ct) =>
        {
            if (learnerId is null)
            {
                return ApiResults.LearnerRequired();
            }

            var result = await handler.CompleteActivityAsync(
                new CompleteActivityCommand(learnerId.Value, sessionId, activityId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result.Error);
        });

        api.MapPost("/sessions/{sessionId:guid}/complete", async (
            [FromHeader(Name = ApiResults.LearnerHeader)] Guid? learnerId,
            Guid sessionId, [FromServices] SessionLifecycleHandler handler, CancellationToken ct) =>
        {
            if (learnerId is null)
            {
                return ApiResults.LearnerRequired();
            }

            var result = await handler.CompleteAsync(new SessionCommand(learnerId.Value, sessionId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result.Error);
        });

        api.MapPost("/sessions/{sessionId:guid}/abandon", async (
            [FromHeader(Name = ApiResults.LearnerHeader)] Guid? learnerId,
            Guid sessionId, [FromServices] SessionLifecycleHandler handler, CancellationToken ct) =>
        {
            if (learnerId is null)
            {
                return ApiResults.LearnerRequired();
            }

            var result = await handler.AbandonAsync(new SessionCommand(learnerId.Value, sessionId), ct);
            return result.IsSuccess ? Results.NoContent() : ApiResults.Problem(result.Error);
        });

        // No session id in the route: the backend routes the exercise to its owning session (doc 12 §2).
        api.MapPost("/exercises/{exerciseId:guid}/attempts", async (
            [FromHeader(Name = ApiResults.LearnerHeader)] Guid? learnerId,
            Guid exerciseId, RecordAttemptRequest request, [FromServices] RecordAttemptHandler handler, CancellationToken ct) =>
        {
            if (learnerId is null)
            {
                return ApiResults.LearnerRequired();
            }

            var result = await handler.HandleAsync(
                new RecordAttemptCommand(
                    learnerId.Value, exerciseId, request.Response, request.HintsUsed, request.LatencyMs),
                ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result.Error);
        });

        return api;
    }
}
