using Lif.Domain.Learning;

namespace Lif.Application.Sessions;

/// <summary>Exercise projection; the answer key never leaves the backend (doc 12 §3).</summary>
public sealed record ExerciseDto(
    Guid ExerciseInstanceId,
    int OrderIndex,
    string Type,
    string Stem,
    IReadOnlyList<string> Options,
    string Source);

public sealed record ActivityDto(
    Guid ActivityId,
    int OrderIndex,
    string Kind,
    string Status,
    int EstimatedMinutes,
    IReadOnlyList<ExerciseDto> Exercises);

/// <summary>Full session projection with the visible plan + rationale (doc 12 §3).</summary>
public sealed record SessionDto(
    Guid SessionId,
    Guid LearnerId,
    string Status,
    int TimeBudgetMinutes,
    double ReviewShare,
    double NewShare,
    IReadOnlyList<string> Rationale,
    string PlannerVersion,
    IReadOnlyList<ActivityDto> Activities);

internal static class SessionMapper
{
    public static SessionDto ToDto(StudySession session) =>
        new(
            session.Id,
            session.LearnerId,
            session.Status.ToString(),
            session.TimeBudgetMinutes,
            session.Plan.ReviewShare,
            session.Plan.NewShare,
            session.Plan.Rationale,
            session.Plan.PlannerVersion,
            session.Activities
                .OrderBy(a => a.OrderIndex)
                .Select(a => new ActivityDto(
                    a.Id,
                    a.OrderIndex,
                    a.Kind.ToString(),
                    a.Status.ToString(),
                    a.EstimatedMinutes,
                    a.Exercises
                        .OrderBy(e => e.OrderIndex)
                        .Select(e => new ExerciseDto(
                            e.Id,
                            e.OrderIndex,
                            e.Spec.Type.ToString(),
                            e.Spec.Stem,
                            e.Spec.Options,
                            e.Source.ToString()))
                        .ToList()))
                .ToList());
}
