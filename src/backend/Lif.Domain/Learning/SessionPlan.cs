namespace Lif.Domain.Learning;

/// <summary>One planned activity inside a <see cref="SessionPlan"/>.</summary>
public sealed record ActivityPlan(
    ActivityKind Kind,
    int EstimatedMinutes,
    IReadOnlyList<Guid> ConceptIds,
    int ItemCount);

/// <summary>
/// Immutable planner output (doc 06 §3.5, doc 12 §3): activity list, share summary,
/// user-visible rationale, and the seed that makes the plan reproducible.
/// </summary>
public sealed record SessionPlan(
    double ReviewShare,
    double NewShare,
    double FreeShare,
    IReadOnlyList<ActivityPlan> Activities,
    IReadOnlyList<string> Rationale,
    int Seed,
    string PlannerVersion);
