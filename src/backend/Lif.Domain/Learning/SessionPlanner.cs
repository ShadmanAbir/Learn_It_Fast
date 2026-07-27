namespace Lif.Domain.Learning;

/// <summary>A frontier concept offered to the planner, pre-ranked by the FrontierCalculator.</summary>
public sealed record PlannerCandidate(Guid ConceptId, string Code, string Title, int EstimatedMinutes, int UnlockCount);

/// <summary>Immutable planner input snapshot; everything the planner may consider.</summary>
public sealed record SessionPlannerInput(
    int TimeBudgetMinutes,
    int DueReviewCount,
    IReadOnlyList<PlannerCandidate> FrontierCandidates,
    int Seed);

/// <summary>
/// Pure, seeded session planner v1 (doc 06 §6): composes review + lesson activities from
/// the frontier and the due queue, and explains itself (rationale is user-visible, doc 12 §3).
/// The AI never plans — it is only ever asked to render content for a plan that already exists.
/// </summary>
public static class SessionPlanner
{
    public const string Version = "1.0.0";

    /// <summary>Review pacing assumption: items a learner clears per minute.</summary>
    public const int ReviewItemsPerMinute = 2;

    /// <summary>Budget share reserved for reviews when reviews are due.</summary>
    public const double ReviewShare = 0.4;

    /// <summary>Max concepts introduced in one session (matches LessonTemplate's 1–3).</summary>
    public const int MaxNewConcepts = 3;

    public static SessionPlan Plan(SessionPlannerInput input)
    {
        var budget = Math.Max(5, input.TimeBudgetMinutes);
        var activities = new List<ActivityPlan>();
        var rationale = new List<string>();

        // 1. Reviews first — protecting memory beats new material (FSRS discipline).
        var reviewMinutes = 0;
        if (input.DueReviewCount > 0)
        {
            var reviewCap = Math.Max(1, (int)Math.Floor(budget * ReviewShare));
            var minutesNeeded = (int)Math.Ceiling((double)input.DueReviewCount / ReviewItemsPerMinute);
            reviewMinutes = Math.Min(reviewCap, minutesNeeded);
            var itemCount = Math.Min(input.DueReviewCount, reviewMinutes * ReviewItemsPerMinute);

            activities.Add(new ActivityPlan(ActivityKind.Review, reviewMinutes, [], itemCount));
            rationale.Add(itemCount < input.DueReviewCount
                ? $"{itemCount} reviews due (priority-capped from {input.DueReviewCount})"
                : $"{itemCount} reviews due");
        }
        else
        {
            rationale.Add("No reviews due — full budget goes to new material");
        }

        // 2. New concepts from the frontier, in rank order, while budget lasts.
        var lessonBudget = budget - reviewMinutes;
        var chosen = new List<PlannerCandidate>();
        foreach (var candidate in input.FrontierCandidates)
        {
            if (chosen.Count >= MaxNewConcepts)
            {
                break;
            }

            var cost = chosen.Sum(c => c.EstimatedMinutes) + candidate.EstimatedMinutes;
            if (chosen.Count > 0 && cost > lessonBudget)
            {
                break;
            }

            chosen.Add(candidate);
        }

        if (chosen.Count > 0)
        {
            var lessonMinutes = Math.Min(lessonBudget, chosen.Sum(c => c.EstimatedMinutes));
            activities.Add(new ActivityPlan(
                ActivityKind.Lesson, lessonMinutes, chosen.Select(c => c.ConceptId).ToList(), chosen.Count));

            foreach (var candidate in chosen)
            {
                rationale.Add(candidate.UnlockCount > 0
                    ? $"{candidate.Title}: frontier concept, unlocks {candidate.UnlockCount} more"
                    : $"{candidate.Title}: frontier concept");
            }
        }
        else if (input.FrontierCandidates.Count == 0)
        {
            rationale.Add("Frontier is empty — nothing new is unlocked right now");
        }

        var totalMinutes = Math.Max(1, activities.Sum(a => a.EstimatedMinutes));
        var reviewShare = Math.Round((double)reviewMinutes / totalMinutes, 2);

        return new SessionPlan(
            ReviewShare: reviewShare,
            NewShare: Math.Round(1.0 - reviewShare, 2),
            FreeShare: 0.0,
            Activities: activities,
            Rationale: rationale,
            Seed: input.Seed,
            PlannerVersion: Version);
    }
}
