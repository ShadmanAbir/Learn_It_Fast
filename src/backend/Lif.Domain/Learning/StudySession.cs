using Lif.SharedKernel.Domain;
using Lif.SharedKernel.Results;

namespace Lif.Domain.Learning;

public enum SessionStatus
{
    Planned = 1,
    Active = 2,
    Completed = 3,
    Abandoned = 4,
}

/// <summary>
/// One planned study unit (doc 06 §3.5). Invariants: activity order fixed at planning;
/// attempts are immutable; state transitions Planned → Active → Completed|Abandoned.
/// </summary>
public sealed class StudySession : AggregateRoot
{
    private readonly List<Activity> _activities = [];

    private StudySession()
    {
    }

    private StudySession(Guid id, Guid learnerId, int timeBudgetMinutes, SessionPlan plan)
        : base(id)
    {
        LearnerId = learnerId;
        TimeBudgetMinutes = timeBudgetMinutes;
        Plan = plan;
        Status = SessionStatus.Planned;
    }

    public Guid LearnerId { get; private set; }

    public SessionStatus Status { get; private set; }

    public int TimeBudgetMinutes { get; private set; }

    /// <summary>Planner snapshot (reproducibility: plan + seed + planner version).</summary>
    public SessionPlan Plan { get; private set; } = null!;

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyList<Activity> Activities => _activities.AsReadOnly();

    /// <summary>Materializes a planned session: one Activity per ActivityPlan, order fixed.</summary>
    public static StudySession FromPlan(Guid learnerId, int timeBudgetMinutes, SessionPlan plan)
    {
        var session = new StudySession(Guid.CreateVersion7(), learnerId, timeBudgetMinutes, plan);
        foreach (var (activityPlan, index) in plan.Activities.Select((a, i) => (a, i)))
        {
            session._activities.Add(Activity.Create(session.Id, index, activityPlan.Kind, null, activityPlan.EstimatedMinutes));
        }

        return session;
    }

    public Result Start(DateTimeOffset now)
    {
        if (Status != SessionStatus.Planned)
        {
            return Result.Failure(LearningErrors.SessionNotPlanned);
        }

        Status = SessionStatus.Active;
        StartedAt = now;
        if (_activities.Count > 0)
        {
            _activities[0].MarkActive();
        }

        return Result.Success();
    }

    /// <summary>
    /// Evaluates deterministically, appends the immutable attempt, and raises
    /// <see cref="AttemptRecorded"/> for mastery/scheduling/gamification consumers.
    /// </summary>
    public Result<Attempt> RecordAttempt(Guid exerciseInstanceId, string response, int hintsUsed, int? latencyMs, DateTimeOffset now)
    {
        if (Status != SessionStatus.Active)
        {
            return Result.Failure<Attempt>(LearningErrors.SessionNotActive);
        }

        var exercise = _activities.SelectMany(a => a.Exercises).FirstOrDefault(e => e.Id == exerciseInstanceId);
        if (exercise is null)
        {
            return Result.Failure<Attempt>(LearningErrors.ExerciseNotFound);
        }

        var evaluation = ExerciseEvaluator.Evaluate(exercise.Spec, response);
        var attempt = exercise.AddAttempt(LearnerId, response, evaluation.Outcome, hintsUsed, latencyMs, "deterministic", now);

        Raise(new AttemptRecorded(
            LearnerId, Id, exercise.Id, exercise.Spec.Type, exercise.Spec.ConceptIds,
            exercise.Spec.Options.Count, evaluation.Outcome, hintsUsed, now));

        return attempt;
    }

    public Result CompleteActivity(Guid activityId)
    {
        if (Status != SessionStatus.Active)
        {
            return Result.Failure(LearningErrors.SessionNotActive);
        }

        var activity = _activities.FirstOrDefault(a => a.Id == activityId);
        if (activity is null)
        {
            return Result.Failure(LearningErrors.ActivityNotFound);
        }

        activity.MarkCompleted();
        var next = _activities.Where(a => a.Status == ActivityStatus.Pending).OrderBy(a => a.OrderIndex).FirstOrDefault();
        next?.MarkActive();
        return Result.Success();
    }

    public Result Complete(DateTimeOffset now)
    {
        if (Status != SessionStatus.Active)
        {
            return Result.Failure(LearningErrors.SessionNotActive);
        }

        foreach (var activity in _activities.Where(a => a.Status is ActivityStatus.Pending or ActivityStatus.Active))
        {
            activity.MarkSkipped();
        }

        Status = SessionStatus.Completed;
        CompletedAt = now;

        var attempts = _activities.SelectMany(a => a.Exercises).SelectMany(e => e.Attempts).ToList();
        var average = attempts.Count > 0 ? attempts.Average(a => a.Outcome) : 0.0;
        Raise(new SessionCompleted(LearnerId, Id, attempts.Count, average, now));
        return Result.Success();
    }

    public Result Abandon(DateTimeOffset now)
    {
        if (Status is SessionStatus.Completed or SessionStatus.Abandoned)
        {
            return Result.Failure(LearningErrors.SessionAlreadyClosed);
        }

        Status = SessionStatus.Abandoned;
        CompletedAt = now;
        return Result.Success();
    }
}
