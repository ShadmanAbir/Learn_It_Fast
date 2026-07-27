using Lif.Domain.Scheduling;

namespace Lif.Application.Abstractions;

/// <summary>Unified review queue store (words + concept drills).</summary>
public interface IReviewItemRepository
{
    public Task<ReviewItem?> GetAsync(Guid reviewItemId, CancellationToken cancellationToken = default);

    /// <summary>Existing item for a learner/kind/ref triple (idempotent introduction).</summary>
    public Task<ReviewItem?> FindAsync(Guid learnerId, ReviewItemKind kind, Guid itemRef, CancellationToken cancellationToken = default);

    public Task<int> CountDueAsync(Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<ReviewItem>> GetDueAsync(Guid learnerId, DateTimeOffset now, int limit, CancellationToken cancellationToken = default);

    public void Add(ReviewItem item);

    public void AddLog(ReviewLog log);
}
