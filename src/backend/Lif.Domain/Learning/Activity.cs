using Lif.SharedKernel.Domain;

namespace Lif.Domain.Learning;

/// <summary>Kind of activity inside a session; Phase 1 ships Review and Lesson.</summary>
public enum ActivityKind
{
    Review = 1,
    Lesson = 2,
    Reading = 3,
    Listening = 4,
    Writing = 5,
    Conversation = 6,
}

public enum ActivityStatus
{
    Pending = 1,
    Active = 2,
    Completed = 3,
    Skipped = 4,
}

/// <summary>One planned block inside a study session; order is fixed at planning time.</summary>
public sealed class Activity : Entity
{
    private readonly List<ExerciseInstance> _exercises = [];

    private Activity()
    {
    }

    private Activity(Guid id, Guid sessionId, int orderIndex, ActivityKind kind, Guid? refId, int estimatedMinutes)
        : base(id)
    {
        SessionId = sessionId;
        OrderIndex = orderIndex;
        Kind = kind;
        RefId = refId;
        EstimatedMinutes = estimatedMinutes;
        Status = ActivityStatus.Pending;
    }

    public Guid SessionId { get; private set; }

    public int OrderIndex { get; private set; }

    public ActivityKind Kind { get; private set; }

    /// <summary>Lesson template / reading text / scenario reference, per kind.</summary>
    public Guid? RefId { get; private set; }

    public int EstimatedMinutes { get; private set; }

    public ActivityStatus Status { get; private set; }

    public IReadOnlyList<ExerciseInstance> Exercises => _exercises.AsReadOnly();

    internal static Activity Create(Guid sessionId, int orderIndex, ActivityKind kind, Guid? refId, int estimatedMinutes) =>
        new(Guid.CreateVersion7(), sessionId, orderIndex, kind, refId, estimatedMinutes);

    public ExerciseInstance AddCuratedExercise(ExerciseSpec spec)
    {
        var exercise = ExerciseInstance.CreateCurated(Id, _exercises.Count, spec);
        _exercises.Add(exercise);
        return exercise;
    }

    public ExerciseInstance AddAiGeneratedExercise(ExerciseSpec spec, string generationMeta)
    {
        var exercise = ExerciseInstance.CreateAiGenerated(Id, _exercises.Count, spec, generationMeta);
        _exercises.Add(exercise);
        return exercise;
    }

    internal void MarkActive() => Status = ActivityStatus.Active;

    internal void MarkCompleted() => Status = ActivityStatus.Completed;

    internal void MarkSkipped() => Status = ActivityStatus.Skipped;
}
