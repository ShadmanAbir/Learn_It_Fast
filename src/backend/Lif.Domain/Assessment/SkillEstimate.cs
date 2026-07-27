using Lif.SharedKernel.Domain;
using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Assessment;

/// <summary>
/// Per-learner, per-skill ability estimate (doc 06 §3.3). The observation stream is the
/// source of truth; this aggregate is the O(1) read model kept current via <see cref="Apply"/>.
/// Invariant: theta and level only change through applied observations.
/// </summary>
public sealed class SkillEstimate : AggregateRoot
{
    /// <summary>Minimum observations since the last level change before it may move again (FR-ASM-3).</summary>
    public const int MinObservationsForLevelChange = 30;

    /// <summary>Minimum derivation confidence before a level change is allowed (FR-ASM-3).</summary>
    public const double MinConfidenceForLevelChange = 0.7;

    private SkillEstimate()
    {
    }

    private SkillEstimate(Guid id, Guid learnerId, Skill skill, double theta, bool provisional)
        : base(id)
    {
        LearnerId = learnerId;
        Skill = skill;
        Theta = theta;
        Level = CefrDeriver.LevelFor(theta);
        Provisional = provisional;
    }

    public Guid LearnerId { get; private set; }

    public Skill Skill { get; private set; }

    /// <summary>Ability on the continuous CEFR-rank scale (see <see cref="AbilityUpdater"/>).</summary>
    public double Theta { get; private set; }

    /// <summary>Accumulated Fisher information; SE and confidence are derived from it.</summary>
    public double Information { get; private set; }

    public int ObservationCount { get; private set; }

    /// <summary>Observations applied since the last level change; gates the next one.</summary>
    public int ObservationsSinceLevelChange { get; private set; }

    /// <summary>Confidence-gated CEFR level — derived, never assigned directly (ADR-006).</summary>
    public CefrLevel Level { get; private set; }

    /// <summary>Placement-seeded estimate not yet confirmed by regular observations.</summary>
    public bool Provisional { get; private set; }

    public DateTimeOffset? LastObservedAt { get; private set; }

    public double StandardError => AbilityUpdater.StandardError(Information);

    public double Confidence => CefrDeriver.Confidence(StandardError);

    public static SkillEstimate Start(Guid learnerId, Skill skill) =>
        new(Guid.CreateVersion7(), learnerId, skill, theta: 0.0, provisional: false);

    /// <summary>Seeds theta from a finished placement session (FR-ASM-4).</summary>
    public static SkillEstimate SeedFromPlacement(Guid learnerId, Skill skill, double theta) =>
        new(
            Guid.CreateVersion7(),
            learnerId,
            skill,
            Math.Clamp(theta, AbilityUpdater.ThetaMin, AbilityUpdater.ThetaMax),
            provisional: true);

    /// <summary>Applies one observation via the Elo/IRT-lite updater. The only mutation path.</summary>
    public void Apply(Observation observation)
    {
        if (observation.LearnerId != LearnerId || observation.Skill != Skill)
        {
            throw new InvalidOperationException("Observation does not belong to this skill estimate.");
        }

        var previousTheta = Theta;
        var k = AbilityUpdater.KFactor(ObservationCount);

        Information += AbilityUpdater.InformationGain(Theta, observation.ItemDifficulty, observation.Weight);
        Theta = AbilityUpdater.Update(Theta, observation.ItemDifficulty, observation.Outcome, observation.Weight, k);

        ObservationCount++;
        ObservationsSinceLevelChange++;
        Provisional = false;
        LastObservedAt = observation.OccurredAt;

        Raise(new SkillEstimateUpdated(
            LearnerId, Skill, previousTheta, Theta, StandardError, ObservationCount, observation.OccurredAt));

        TryChangeLevel(observation.OccurredAt);
    }

    /// <summary>FR-ASM-3 gate: enough observations since the last change and confidence ≥ 0.7.</summary>
    private void TryChangeLevel(DateTimeOffset now)
    {
        var derived = CefrDeriver.LevelFor(Theta);
        if (derived == Level
            || ObservationsSinceLevelChange < MinObservationsForLevelChange
            || Confidence < MinConfidenceForLevelChange)
        {
            return;
        }

        var previous = Level;
        Level = derived;
        ObservationsSinceLevelChange = 0;

        Raise(new SkillLevelChanged(LearnerId, Skill, previous, Level, Theta, Confidence, now));
    }
}
