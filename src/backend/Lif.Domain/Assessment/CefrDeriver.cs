using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Assessment;

/// <summary>
/// Pure CEFR derivation from theta bands (ADR-006, doc 09 §2). Levels are always derived —
/// never set directly and never taken from LLM output (ADR-001).
/// </summary>
public static class CefrDeriver
{
    /// <summary>SE at (or above) which confidence is zero; equals the prior SE of a fresh estimate.</summary>
    public const double SaturationStandardError = 2.0;

    /// <summary>Theta band → level: [0,1) = A1.1 … [9,∞) = C1.2; below 0 clamps to A1.1.</summary>
    public static CefrLevel LevelFor(double theta) =>
        CefrLevel.FromRank(Math.Clamp((int)Math.Floor(theta), 0, 9));

    /// <summary>Confidence ∈ [0,1]: 1 − SE/2, floored at 0. Gates level changes (FR-ASM-3).</summary>
    public static double Confidence(double standardError) =>
        1.0 - Math.Min(1.0, Math.Max(0.0, standardError) / SaturationStandardError);

    /// <summary>
    /// Overall theta = observation-count-weighted mean over skills with evidence.
    /// Null when nothing has been observed yet (the UI shows "unplaced").
    /// </summary>
    public static double? OverallTheta(IEnumerable<(double Theta, int ObservationCount)> estimates)
    {
        double weighted = 0;
        long total = 0;
        foreach (var (theta, count) in estimates)
        {
            if (count <= 0)
            {
                continue;
            }

            weighted += theta * count;
            total += count;
        }

        return total > 0 ? weighted / total : null;
    }
}
