namespace Lif.Domain.Scheduling;

/// <summary>
/// Pure FSRS-inspired scheduler v1 (doc 06 §6). Intervals target 90 % retention:
/// with R(t) = (1 + t/(9·S))⁻¹, scheduling at t = S days gives exactly R = 0.9.
/// Per-learner parameter optimization is a post-v1 track; these defaults are unit-tested
/// for monotonicity (Easy ≥ Good ≥ Hard, lapse shrinks stability).
/// </summary>
public static class FsrsScheduler
{
    // Initial stability per first rating (days) — FSRS default-inspired.
    private static readonly double[] InitialStability = [0.4, 0.6, 2.4, 5.8];

    private const double InitialDifficultyBase = 4.93;
    private const double InitialDifficultySlope = 0.94;

    /// <summary>Computes the next memory state after a review at <paramref name="now"/>.</summary>
    public static FsrsState Next(FsrsState? state, ReviewRating rating, DateTimeOffset now)
    {
        if (state is null || state.LastReviewAt is null)
        {
            return First(rating, now);
        }

        var difficulty = NextDifficulty(state.Difficulty, rating);
        var retrievability = state.Retrievability(now);

        double stability;
        if (rating == ReviewRating.Again)
        {
            // Lapse: stability collapses but retains a memory trace.
            stability = Math.Max(0.3, 0.5 * Math.Sqrt(state.Stability));
        }
        else
        {
            // Growth shrinks as difficulty rises, as stability saturates, and as the
            // review happens early (high retrievability → little new information).
            var growth = Math.Exp(0.9)
                         * (11.0 - difficulty)
                         * Math.Pow(Math.Max(state.Stability, 0.01), -0.2)
                         * (Math.Exp(1.3 * (1.0 - retrievability)) - 1.0);

            var multiplier = rating switch
            {
                ReviewRating.Hard => 0.6,
                ReviewRating.Easy => 1.6,
                _ => 1.0,
            };

            stability = state.Stability * (1.0 + (growth * multiplier));
        }

        return new FsrsState(stability, difficulty, now, now.AddDays(stability));
    }

    private static FsrsState First(ReviewRating rating, DateTimeOffset now)
    {
        var stability = InitialStability[(int)rating - 1];
        var difficulty = Math.Clamp(
            InitialDifficultyBase - (((int)rating - 3) * InitialDifficultySlope), 1.0, 10.0);
        return new FsrsState(stability, difficulty, now, now.AddDays(stability));
    }

    private static double NextDifficulty(double difficulty, ReviewRating rating) =>
        Math.Clamp(difficulty - (0.5 * ((int)rating - 3)), 1.0, 10.0);
}
