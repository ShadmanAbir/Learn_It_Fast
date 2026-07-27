using Lif.SharedKernel.Domain;
using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Assessment;

/// <summary>Where an observation was produced (doc 07 §5 source_kind).</summary>
public enum ObservationSource
{
    Exercise = 1,
    Review = 2,
    Placement = 3,
    Exam = 4,
    Writing = 5,
    Conversation = 6,
    Reading = 7,
    Listening = 8,
}

/// <summary>
/// One scoreable interaction outcome in the append-only assessment stream (FR-ASM-1).
/// The stream is the source of truth — skill estimates are recomputable from it (NFR-REL-1).
/// </summary>
public sealed class Observation : Entity
{
    private Observation()
    {
    }

    private Observation(
        Guid id,
        Guid learnerId,
        Skill skill,
        Guid? conceptId,
        double itemDifficulty,
        double outcome,
        double weight,
        ObservationSource sourceKind,
        Guid sourceId,
        DateTimeOffset occurredAt)
        : base(id)
    {
        LearnerId = learnerId;
        Skill = skill;
        ConceptId = conceptId;
        ItemDifficulty = itemDifficulty;
        Outcome = outcome;
        Weight = weight;
        SourceKind = sourceKind;
        SourceId = sourceId;
        OccurredAt = occurredAt;
    }

    public Guid LearnerId { get; private set; }

    public Skill Skill { get; private set; }

    public Guid? ConceptId { get; private set; }

    /// <summary>Item difficulty on the theta scale (continuous CEFR rank, see <see cref="AbilityUpdater"/>).</summary>
    public double ItemDifficulty { get; private set; }

    /// <summary>Outcome ∈ [0,1] (partial credit).</summary>
    public double Outcome { get; private set; }

    /// <summary>Update weight; AI-scored observations flagged by disagreement are down-weighted (FR-ASM-6).</summary>
    public double Weight { get; private set; }

    public ObservationSource SourceKind { get; private set; }

    /// <summary>Attempt / review log / exam item id that produced this outcome.</summary>
    public Guid SourceId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static Observation Capture(
        Guid learnerId,
        Skill skill,
        Guid? conceptId,
        double itemDifficulty,
        double outcome,
        ObservationSource sourceKind,
        Guid sourceId,
        DateTimeOffset occurredAt,
        double weight = 1.0) =>
        new(
            Guid.CreateVersion7(),
            learnerId,
            skill,
            conceptId,
            itemDifficulty,
            Math.Clamp(outcome, 0.0, 1.0),
            Math.Clamp(weight, 0.0, 1.0),
            sourceKind,
            sourceId,
            occurredAt);
}
