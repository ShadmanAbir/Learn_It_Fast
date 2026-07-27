using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Learning;

/// <summary>
/// Immutable exercise definition (doc 06 §3.5). Answer-key encoding per type:
/// MC = correct option text · Cloze = expected fill · Matching = "left=right|left=right" ·
/// Ordering = tokens in correct sequence joined by "|" (options carry the shuffled tokens).
/// </summary>
public sealed record ExerciseSpec(
    ExerciseType Type,
    string Stem,
    IReadOnlyList<string> Options,
    string AnswerKey,
    IReadOnlyList<Guid> ConceptIds,
    Difficulty Difficulty,
    string? Explanation = null,
    string? ErrorCode = null);
