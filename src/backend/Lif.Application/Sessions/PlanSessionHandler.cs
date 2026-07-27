using Lif.Application.Abstractions;
using Lif.Domain.Curriculum;
using Lif.Domain.Learning;
using Lif.SharedKernel.Results;

namespace Lif.Application.Sessions;

/// <summary>POST /sessions (doc 12 §3): plan a session; null budget falls back to the daily goal.</summary>
public sealed record PlanSessionCommand(Guid LearnerId, int? TimeBudgetMinutes = null);

/// <summary>
/// Plans a study session: due reviews + frontier concepts → deterministic plan →
/// materialized activities filled from the curated exercise bank (ADR-008; AI variants join in Phase 1's
/// AI gateway step). The plan and its rationale are user-visible (FR-PLN-6).
/// </summary>
public sealed class PlanSessionHandler(
    ILearnerRepository learners,
    ICurriculumReader curriculum,
    IConceptMasteryRepository masteries,
    IReviewItemRepository reviews,
    IStudySessionRepository sessions,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    /// <summary>Frontier candidates offered to the planner per session.</summary>
    public const int FrontierWindow = 10;

    /// <summary>Curated exercises materialized per lesson concept.</summary>
    public const int ExercisesPerConcept = 5;

    public async Task<Result<SessionDto>> HandleAsync(PlanSessionCommand command, CancellationToken cancellationToken = default)
    {
        var profile = await learners.GetAsync(command.LearnerId, cancellationToken);
        if (profile is null)
        {
            return Result.Failure<SessionDto>(ApplicationErrors.LearnerNotFound);
        }

        var open = await sessions.GetOpenForLearnerAsync(command.LearnerId, cancellationToken);
        if (open is not null)
        {
            return Result.Failure<SessionDto>(ApplicationErrors.SessionAlreadyOpen);
        }

        var now = clock.UtcNow;
        var budget = command.TimeBudgetMinutes ?? profile.MinutesPerDay;

        // Planner inputs: due review count + ranked frontier.
        var dueCount = await reviews.CountDueAsync(command.LearnerId, now, cancellationToken);
        var concepts = await curriculum.GetConceptsAsync(profile.CurriculumVersionId, cancellationToken);
        var unitOrder = await curriculum.GetUnitOrderAsync(profile.CurriculumVersionId, cancellationToken);
        var masteryRows = await masteries.GetAllAsync(command.LearnerId, cancellationToken);
        var masteryByConcept = masteryRows.ToDictionary(m => m.ConceptId, m => m.Mastery);

        var conceptById = concepts.ToDictionary(c => c.Id);
        var frontier = FrontierCalculator.Compute(concepts, masteryByConcept, profile.CurrentLevel, unitOrder);
        var candidates = frontier
            .Take(FrontierWindow)
            .Select(f => new PlannerCandidate(
                f.ConceptId, f.Code, conceptById[f.ConceptId].Title,
                conceptById[f.ConceptId].EstimatedMinutes, f.UnlockCount))
            .ToList();

        // Deterministic given (learner, day): replanning the same day yields the same seed.
        var seed = (int)((command.LearnerId.GetHashCode() ^ now.UtcDateTime.Date.Ticks) & int.MaxValue);
        var plan = SessionPlanner.Plan(new SessionPlannerInput(budget, dueCount, candidates, seed));
        var session = StudySession.FromPlan(command.LearnerId, budget, plan);

        await MaterializeLessonExercisesAsync(session, plan, profile.CurriculumVersionId, cancellationToken);

        sessions.Add(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return SessionMapper.ToDto(session);
    }

    /// <summary>Fills lesson activities from the curated bank, round-robin over the planned concepts.</summary>
    private async Task MaterializeLessonExercisesAsync(
        StudySession session, SessionPlan plan, Guid versionId, CancellationToken cancellationToken)
    {
        var lessonConceptIds = plan.Activities
            .Where(a => a.Kind == ActivityKind.Lesson)
            .SelectMany(a => a.ConceptIds)
            .Distinct()
            .ToList();

        if (lessonConceptIds.Count == 0)
        {
            return;
        }

        var bank = await curriculum.GetExercisesForConceptsAsync(versionId, lessonConceptIds, cancellationToken);
        var byConcept = lessonConceptIds.ToDictionary(
            id => id,
            id => bank
                .Where(e => e.Spec.ConceptIds.Contains(id))
                .OrderBy(e => e.Code, StringComparer.Ordinal)
                .Take(ExercisesPerConcept)
                .ToList());

        foreach (var (activity, activityPlan) in session.Activities.Zip(plan.Activities))
        {
            if (activityPlan.Kind != ActivityKind.Lesson)
            {
                continue; // Review activities are served by the review queue endpoints.
            }

            // Interleave concepts so a two-concept lesson alternates instead of blocking.
            var queues = activityPlan.ConceptIds
                .Select(id => new Queue<CuratedExercise>(byConcept.GetValueOrDefault(id) ?? []))
                .ToList();
            var added = new HashSet<Guid>();
            while (queues.Any(q => q.Count > 0))
            {
                foreach (var queue in queues.Where(q => q.Count > 0))
                {
                    var exercise = queue.Dequeue();
                    if (added.Add(exercise.Id))
                    {
                        activity.AddCuratedExercise(exercise.Spec);
                    }
                }
            }
        }
    }
}
