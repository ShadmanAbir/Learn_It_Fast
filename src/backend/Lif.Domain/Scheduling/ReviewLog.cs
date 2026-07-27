using Lif.SharedKernel.Domain;

namespace Lif.Domain.Scheduling;

/// <summary>Append-only review history; the input for future FSRS parameter optimization.</summary>
public sealed class ReviewLog : Entity
{
    private ReviewLog()
    {
    }

    private ReviewLog(Guid id, Guid reviewItemId, Guid learnerId, ReviewRating rating, double elapsedDays, double scheduledDays, DateTimeOffset reviewedAt)
        : base(id)
    {
        ReviewItemId = reviewItemId;
        LearnerId = learnerId;
        Rating = rating;
        ElapsedDays = elapsedDays;
        ScheduledDays = scheduledDays;
        ReviewedAt = reviewedAt;
    }

    public Guid ReviewItemId { get; private set; }

    public Guid LearnerId { get; private set; }

    public ReviewRating Rating { get; private set; }

    public double ElapsedDays { get; private set; }

    public double ScheduledDays { get; private set; }

    public DateTimeOffset ReviewedAt { get; private set; }

    internal static ReviewLog Record(Guid reviewItemId, Guid learnerId, ReviewRating rating, double elapsedDays, double scheduledDays, DateTimeOffset reviewedAt) =>
        new(Guid.CreateVersion7(), reviewItemId, learnerId, rating, elapsedDays, scheduledDays, reviewedAt);
}
