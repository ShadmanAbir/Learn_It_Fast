using Lif.Application.Abstractions;
using Lif.Domain.Scheduling;
using Lif.SharedKernel.Results;

namespace Lif.Application.Reviews;

/// <summary>GET /reviews/queue (doc 12 §4): due items ordered by priority.</summary>
public sealed record ReviewQueueQuery(Guid LearnerId, int Limit = 50);

public sealed record ReviewQueueItemDto(
    Guid ReviewItemId,
    string Kind,
    Guid ItemRef,
    DateTimeOffset DueAt,
    double Priority);

public sealed record ReviewQueueDto(int DueCount, IReadOnlyList<ReviewQueueItemDto> Items);

/// <summary>
/// Serves the unified review queue: scores every due item with the pure prioritizer and
/// persists the score, so the ordering is inspectable (FR-REV-3, doc 08 §7).
/// </summary>
public sealed class GetReviewQueueHandler(
    IReviewItemRepository reviews,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public const int MaxLimit = 100;

    public async Task<Result<ReviewQueueDto>> HandleAsync(
        ReviewQueueQuery query, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var limit = Math.Clamp(query.Limit, 1, MaxLimit);

        var dueCount = await reviews.CountDueAsync(query.LearnerId, now, cancellationToken);
        var due = await reviews.GetDueAsync(query.LearnerId, now, MaxLimit, cancellationToken);

        // Phase 1 scoring: neutral error-rate/importance/confidence signals — overdueness and
        // forgetting risk dominate. Item-level signals join with the assessment context.
        foreach (var item in due)
        {
            item.SetPriority(ReviewPrioritizer.Score(
                item.State, now, recentErrorRate: 0.0, importance: 0.5, confidence: 0.5));
        }

        var ordered = due
            .OrderByDescending(i => i.Priority)
            .ThenBy(i => i.State.DueAt)
            .ThenBy(i => i.Id)
            .Take(limit)
            .Select(i => new ReviewQueueItemDto(i.Id, i.Kind.ToString(), i.ItemRef, i.State.DueAt, i.Priority))
            .ToList();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ReviewQueueDto(dueCount, ordered);
    }
}
