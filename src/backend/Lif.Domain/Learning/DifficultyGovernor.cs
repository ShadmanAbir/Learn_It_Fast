using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Learning;

/// <summary>
/// Pure difficulty band governor (doc 06 §6): adjusts by at most one band per call,
/// based on the rolling success rate of recent attempts.
/// </summary>
public static class DifficultyGovernor
{
    /// <summary>Minimum observations before the governor reacts.</summary>
    public const int MinWindow = 5;

    public const double StepUpThreshold = 0.85;
    public const double StepDownThreshold = 0.60;

    public static Difficulty Adjust(Difficulty current, IReadOnlyList<double> recentOutcomes)
    {
        if (recentOutcomes.Count < MinWindow)
        {
            return current;
        }

        var successRate = recentOutcomes.Average();
        if (successRate >= StepUpThreshold)
        {
            return current.StepUp();
        }

        return successRate < StepDownThreshold ? current.StepDown() : current;
    }
}
