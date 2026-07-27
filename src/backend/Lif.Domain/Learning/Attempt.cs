using Lif.SharedKernel.Domain;

namespace Lif.Domain.Learning;

/// <summary>Append-only, immutable record of one exercise attempt (doc 06 §3.5).</summary>
public sealed class Attempt : Entity
{
    private Attempt()
    {
    }

    private Attempt(Guid id, Guid exerciseInstanceId, Guid learnerId, string response, double outcome, int hintsUsed, int? latencyMs, string evaluatedBy, DateTimeOffset occurredAt)
        : base(id)
    {
        ExerciseInstanceId = exerciseInstanceId;
        LearnerId = learnerId;
        Response = response;
        Outcome = outcome;
        HintsUsed = hintsUsed;
        LatencyMs = latencyMs;
        EvaluatedBy = evaluatedBy;
        OccurredAt = occurredAt;
    }

    public Guid ExerciseInstanceId { get; private set; }

    public Guid LearnerId { get; private set; }

    public string Response { get; private set; } = string.Empty;

    /// <summary>Partial-credit outcome ∈ [0,1].</summary>
    public double Outcome { get; private set; }

    public int HintsUsed { get; private set; }

    public int? LatencyMs { get; private set; }

    /// <summary>deterministic | ai(provider/model) | hybrid</summary>
    public string EvaluatedBy { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    internal static Attempt Record(Guid exerciseInstanceId, Guid learnerId, string response, double outcome, int hintsUsed, int? latencyMs, string evaluatedBy, DateTimeOffset occurredAt) =>
        new(Guid.CreateVersion7(), exerciseInstanceId, learnerId, response, outcome, hintsUsed, latencyMs, evaluatedBy, occurredAt);
}
