using Lif.SharedKernel.Domain;

namespace Lif.Domain.Scheduling;

/// <summary>What a review item refers to.</summary>
public enum ReviewItemKind
{
    Word = 1,
    ConceptDrill = 2,
}

/// <summary>A reviewable memory unit in the unified queue (doc 06 §3.4).</summary>
public sealed class ReviewItem : AggregateRoot
{
    private ReviewItem()
    {
    }

    private ReviewItem(Guid id, Guid learnerId, ReviewItemKind kind, Guid itemRef, FsrsState state)
        : base(id)
    {
        LearnerId = learnerId;
        Kind = kind;
        ItemRef = itemRef;
        State = state;
    }

    public Guid LearnerId { get; private set; }

    public ReviewItemKind Kind { get; private set; }

    /// <summary>VocabEntry id (Word) or Concept id (ConceptDrill).</summary>
    public Guid ItemRef { get; private set; }

    public FsrsState State { get; private set; } = null!;

    /// <summary>Priority score recomputed by <see cref="ReviewPrioritizer"/>; persisted for queue queries.</summary>
    public double Priority { get; private set; }

    public int PostponedCount { get; private set; }

    /// <summary>Introduces an item to the queue: first exposure is scheduled as a first Good review.</summary>
    public static ReviewItem Introduce(Guid learnerId, ReviewItemKind kind, Guid itemRef, DateTimeOffset now)
    {
        var state = FsrsScheduler.Next(null, ReviewRating.Good, now);
        return new ReviewItem(Guid.CreateVersion7(), learnerId, kind, itemRef, state);
    }

    /// <summary>Applies a review answer and returns the log entry to append.</summary>
    public ReviewLog Answer(ReviewRating rating, DateTimeOffset now)
    {
        var scheduledDays = State.Stability;
        var elapsedDays = State.LastReviewAt is { } last ? Math.Max(0.0, (now - last).TotalDays) : 0.0;

        State = FsrsScheduler.Next(State, rating, now);
        PostponedCount = 0;

        return ReviewLog.Record(Id, LearnerId, rating, elapsedDays, scheduledDays, now);
    }

    public void Postpone() => PostponedCount++;

    public void SetPriority(double priority) => Priority = priority;
}
