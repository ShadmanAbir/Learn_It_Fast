using Lif.SharedKernel.Domain;
using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Learner;

/// <summary>Where a mistake was observed.</summary>
public enum MistakeSource
{
    Exercise = 1,
    Writing = 2,
    Conversation = 3,
    Review = 4,
}

/// <summary>
/// Append-only mistake record (doc 06 §3.2). Every error maps to a concept or stays
/// explicitly unmapped — mistakes drive remediation and the weakness report.
/// </summary>
public sealed class MistakeRecord : Entity
{
    private MistakeRecord()
    {
    }

    private MistakeRecord(
        Guid id,
        Guid learnerId,
        Skill skill,
        string errorCode,
        Guid? conceptId,
        MistakeSource sourceKind,
        Guid sourceId,
        string? wrong,
        string? right,
        DateTimeOffset occurredAt)
        : base(id)
    {
        LearnerId = learnerId;
        Skill = skill;
        ErrorCode = errorCode;
        ConceptId = conceptId;
        SourceKind = sourceKind;
        SourceId = sourceId;
        Wrong = wrong;
        Right = right;
        OccurredAt = occurredAt;
    }

    public Guid LearnerId { get; private set; }

    public Skill Skill { get; private set; }

    /// <summary>Error taxonomy code: CASE_DAT, ART_GEN, WO_V2 …</summary>
    public string ErrorCode { get; private set; } = string.Empty;

    /// <summary>Mapped concept; null = unmapped (still counted, never silently dropped).</summary>
    public Guid? ConceptId { get; private set; }

    public MistakeSource SourceKind { get; private set; }

    public Guid SourceId { get; private set; }

    public string? Wrong { get; private set; }

    public string? Right { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static MistakeRecord Log(
        Guid learnerId,
        Skill skill,
        string errorCode,
        Guid? conceptId,
        MistakeSource sourceKind,
        Guid sourceId,
        DateTimeOffset occurredAt,
        string? wrong = null,
        string? right = null) =>
        new(Guid.CreateVersion7(), learnerId, skill, errorCode, conceptId, sourceKind, sourceId, wrong, right, occurredAt);
}
