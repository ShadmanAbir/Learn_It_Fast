namespace Lif.Domain.Scheduling;

/// <summary>Weights for the review priority formula (doc 08 §7). Defaults are config-overridable.</summary>
public sealed record ReviewPriorityWeights(
    double Overdueness = 0.30,
    double RecentErrorRate = 0.25,
    double Importance = 0.15,
    double ForgettingRisk = 0.20,
    double Uncertainty = 0.10)
{
    public static readonly ReviewPriorityWeights Default = new();
}

/// <summary>Pure priority score used to order the unified review queue.</summary>
public static class ReviewPrioritizer
{
    /// <param name="state">FSRS state of the item.</param>
    /// <param name="now">Current time.</param>
    /// <param name="recentErrorRate">Error rate over the last ~20 attempts, ∈ [0,1].</param>
    /// <param name="importance">Normalized node importance ∈ [0,1].</param>
    /// <param name="confidence">Estimate confidence ∈ [0,1].</param>
    /// <param name="weights">Formula weights; defaults per doc 08 §7.</param>
    public static double Score(
        FsrsState state,
        DateTimeOffset now,
        double recentErrorRate,
        double importance,
        double confidence,
        ReviewPriorityWeights? weights = null)
    {
        var w = weights ?? ReviewPriorityWeights.Default;

        // Overdueness: (now − due)/interval, capped at 1.
        var intervalDays = Math.Max(state.Stability, 0.01);
        var overdueDays = Math.Max(0.0, (now - state.DueAt).TotalDays);
        var overdueness = Math.Min(1.0, overdueDays / intervalDays);

        return (w.Overdueness * overdueness)
               + (w.RecentErrorRate * Math.Clamp(recentErrorRate, 0, 1))
               + (w.Importance * Math.Clamp(importance, 0, 1))
               + (w.ForgettingRisk * (1.0 - state.Retrievability(now)))
               + (w.Uncertainty * (1.0 - Math.Clamp(confidence, 0, 1)));
    }
}
