using Lif.SharedKernel.Domain;

namespace Lif.Domain.Learning;

/// <summary>Provenance of an exercise instance.</summary>
public enum ExerciseSource
{
    Curated = 1,
    AiGenerated = 2,
}

/// <summary>
/// A concrete exercise inside an activity (doc 06 §3.5). AI-generated instances must
/// reference the validation report that admitted them (<see cref="GenerationMeta"/>).
/// </summary>
public sealed class ExerciseInstance : Entity
{
    private readonly List<Attempt> _attempts = [];

    private ExerciseInstance()
    {
    }

    private ExerciseInstance(Guid id, Guid activityId, int orderIndex, ExerciseSpec spec, ExerciseSource source, string? generationMeta)
        : base(id)
    {
        ActivityId = activityId;
        OrderIndex = orderIndex;
        Spec = spec;
        Source = source;
        GenerationMeta = generationMeta;
    }

    public Guid ActivityId { get; private set; }

    public int OrderIndex { get; private set; }

    public ExerciseSpec Spec { get; private set; } = null!;

    public ExerciseSource Source { get; private set; }

    /// <summary>JSON: template version, context hash, validation report (AI-generated only).</summary>
    public string? GenerationMeta { get; private set; }

    public IReadOnlyList<Attempt> Attempts => _attempts.AsReadOnly();

    internal static ExerciseInstance CreateCurated(Guid activityId, int orderIndex, ExerciseSpec spec) =>
        new(Guid.CreateVersion7(), activityId, orderIndex, spec, ExerciseSource.Curated, null);

    internal static ExerciseInstance CreateAiGenerated(Guid activityId, int orderIndex, ExerciseSpec spec, string generationMeta) =>
        new(Guid.CreateVersion7(), activityId, orderIndex, spec, ExerciseSource.AiGenerated, generationMeta);

    internal Attempt AddAttempt(Guid learnerId, string response, double outcome, int hintsUsed, int? latencyMs, string evaluatedBy, DateTimeOffset now)
    {
        var attempt = Attempt.Record(Id, learnerId, response, outcome, hintsUsed, latencyMs, evaluatedBy, now);
        _attempts.Add(attempt);
        return attempt;
    }
}
