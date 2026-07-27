namespace Lif.Domain.Scheduling;

/// <summary>
/// FSRS memory state for one reviewable item. Stability is measured in days;
/// difficulty is on the FSRS 1–10 scale (distinct from authoring <c>Difficulty</c>).
/// </summary>
public sealed record FsrsState(
    double Stability,
    double Difficulty,
    DateTimeOffset? LastReviewAt,
    DateTimeOffset DueAt)
{
    /// <summary>
    /// Probability of successful recall at <paramref name="now"/> (FSRS forgetting curve):
    /// R(t) = (1 + t/(9·S))⁻¹ — R = 0.9 exactly when t = S.
    /// </summary>
    public double Retrievability(DateTimeOffset now)
    {
        if (LastReviewAt is null)
        {
            return 1.0;
        }

        var elapsedDays = Math.Max(0.0, (now - LastReviewAt.Value).TotalDays);
        return 1.0 / (1.0 + (elapsedDays / (9.0 * Math.Max(Stability, 0.01))));
    }

    public bool IsDue(DateTimeOffset now) => DueAt <= now;
}
