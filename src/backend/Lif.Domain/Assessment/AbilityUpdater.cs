using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Assessment;

/// <summary>
/// Pure Elo/IRT-lite ability update (ADR-006, FR-ASM-2). Theta lives on the continuous
/// CEFR-rank scale (A1.1 entry = 0 … C1.2 = 9); item difficulty is mapped onto the same scale.
/// The K-factor decays with observation count; placement uses an aggressive constant K.
/// </summary>
public static class AbilityUpdater
{
    /// <summary>Theta bounds: one band of headroom below A1.1 and above C1.2.</summary>
    public const double ThetaMin = -1.0;
    public const double ThetaMax = 10.0;

    public const double KMax = 0.60;
    public const double KMin = 0.08;

    /// <summary>Observation count at which K has halved: K = KMax / (1 + n/KDecay).</summary>
    public const double KDecay = 8.0;

    /// <summary>Aggressive constant K for placement items (ADR-006).</summary>
    public const double PlacementK = 1.0;

    /// <summary>Prior information; SE starts at 1/√I0 = 2.0 (one full CEFR major level).</summary>
    public const double BaselineInformation = 0.25;

    /// <summary>P(success) under the 1-parameter logistic model.</summary>
    public static double Expected(double theta, double itemDifficulty) =>
        1.0 / (1.0 + Math.Exp(-(theta - itemDifficulty)));

    /// <summary>Decaying K-factor: large while the estimate is young, floors at <see cref="KMin"/>.</summary>
    public static double KFactor(int observationCount) =>
        Math.Max(KMin, KMax / (1.0 + (Math.Max(0, observationCount) / KDecay)));

    /// <summary>Elo-style update: theta moves by k·w·(outcome − expected), clamped to the scale.</summary>
    public static double Update(double theta, double itemDifficulty, double outcome, double weight, double k)
    {
        var expected = Expected(theta, itemDifficulty);
        var delta = k * Math.Clamp(weight, 0.0, 1.0) * (Math.Clamp(outcome, 0.0, 1.0) - expected);
        return Math.Clamp(theta + delta, ThetaMin, ThetaMax);
    }

    /// <summary>Fisher information contributed by one observation: w·p·(1−p).</summary>
    public static double InformationGain(double theta, double itemDifficulty, double weight)
    {
        var p = Expected(theta, itemDifficulty);
        return Math.Clamp(weight, 0.0, 1.0) * p * (1.0 - p);
    }

    /// <summary>SE = 1/√(I0 + I); drives placement termination (FR-ASM-4) and confidence.</summary>
    public static double StandardError(double information) =>
        1.0 / Math.Sqrt(BaselineInformation + Math.Max(0.0, information));

    /// <summary>Maps authored (level, difficulty 1–10) onto the theta scale: rank + (d−1)/9.</summary>
    public static double ItemTheta(CefrLevel level, Difficulty difficulty) =>
        level.Rank + ((difficulty.Value - 1) / 9.0);
}
