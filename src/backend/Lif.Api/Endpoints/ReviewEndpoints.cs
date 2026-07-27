using Lif.Application.Reviews;
using Microsoft.AspNetCore.Mvc;

namespace Lif.Api.Endpoints;

/// <summary>Request body for POST /reviews/{itemId}/answers; rating: 1=again 2=hard 3=good 4=easy.</summary>
public sealed record AnswerReviewRequest(int Rating, int? ElapsedMs = null);

/// <summary>The unified review queue (doc 12 §2 Reviews; FR-REV-3).</summary>
public static class ReviewEndpoints
{
    public static RouteGroupBuilder MapReviewEndpoints(this RouteGroupBuilder api)
    {
        // Handlers are [FromServices] so hosts without a database (health-only boot) still route.
        api.MapGet("/reviews/queue", async (
            [FromHeader(Name = ApiResults.LearnerHeader)] Guid? learnerId,
            [FromQuery] int? limit, [FromServices] GetReviewQueueHandler handler, CancellationToken ct) =>
        {
            if (learnerId is null)
            {
                return ApiResults.LearnerRequired();
            }

            var result = await handler.HandleAsync(new ReviewQueueQuery(learnerId.Value, limit ?? 50), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result.Error);
        });

        api.MapPost("/reviews/{itemId:guid}/answers", async (
            [FromHeader(Name = ApiResults.LearnerHeader)] Guid? learnerId,
            Guid itemId, AnswerReviewRequest request, [FromServices] AnswerReviewHandler handler, CancellationToken ct) =>
        {
            if (learnerId is null)
            {
                return ApiResults.LearnerRequired();
            }

            var result = await handler.HandleAsync(
                new AnswerReviewCommand(learnerId.Value, itemId, request.Rating, request.ElapsedMs), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : ApiResults.Problem(result.Error);
        });

        return api;
    }
}
