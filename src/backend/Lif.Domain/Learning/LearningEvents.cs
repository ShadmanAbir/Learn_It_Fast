using Lif.SharedKernel.Domain;

namespace Lif.Domain.Learning;

/// <summary>
/// Raised for every scored attempt; consumed by Learner (mastery), Assessment (observation),
/// Scheduling (FSRS) and Gamification (XP) — doc 06 §5.
/// </summary>
public sealed record AttemptRecorded(
    Guid LearnerId,
    Guid SessionId,
    Guid ExerciseInstanceId,
    ExerciseType ExerciseType,
    IReadOnlyList<Guid> ConceptIds,
    int OptionCount,
    double Outcome,
    int HintsUsed,
    DateTimeOffset OccurredAt) : IDomainEvent;

/// <summary>Raised when a study session completes; consumed by Gamification (streak) and Analytics.</summary>
public sealed record SessionCompleted(
    Guid LearnerId,
    Guid SessionId,
    int AttemptCount,
    double AverageOutcome,
    DateTimeOffset OccurredAt) : IDomainEvent;
