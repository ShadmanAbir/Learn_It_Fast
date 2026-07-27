using Lif.SharedKernel.Domain;
using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Assessment;

/// <summary>Raised on every applied observation; feeds estimate snapshots (doc 07 §5).</summary>
public sealed record SkillEstimateUpdated(
    Guid LearnerId,
    Skill Skill,
    double PreviousTheta,
    double NewTheta,
    double StandardError,
    int ObservationCount,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised only when the confidence-gated CEFR level actually moves (FR-ASM-3).</summary>
public sealed record SkillLevelChanged(
    Guid LearnerId,
    Skill Skill,
    CefrLevel PreviousLevel,
    CefrLevel NewLevel,
    double Theta,
    double Confidence,
    DateTimeOffset OccurredAt) : IDomainEvent;
