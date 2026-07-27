using Lif.SharedKernel.Results;

namespace Lif.Application.Ai;

/// <summary>A generated concept explanation: markdown body + example sentences with gloss (doc 11 §4, explanation.v1).</summary>
public sealed record ExplanationDraft(string Markdown, IReadOnlyList<ExampleSentence> Examples);

/// <summary>One German example with its English gloss.</summary>
public sealed record ExampleSentence(string De, string En);

/// <summary>A generated exercise draft; display-only until validated, never affects planning (doc 10 §5).</summary>
public sealed record ExerciseDraft(
    string Type,
    string Stem,
    IReadOnlyList<string> Options,
    string AnswerKey,
    string? Explanation);

/// <summary>
/// Task port: renders a concept explanation for a learner context (doc 10 §1 tier 1).
/// Implementations decide HOW (templates, providers); callers decide WHAT and WHEN.
/// </summary>
public interface IExplainer
{
    public Task<Result<ExplanationDraft>> ExplainAsync(
        string conceptTitle, ContextPack context, CancellationToken cancellationToken = default);
}

/// <summary>Task port: generates exercise variants for an already-planned concept (ADR-008).</summary>
public interface IExerciseGenerator
{
    public Task<Result<IReadOnlyList<ExerciseDraft>>> GenerateAsync(
        string exerciseType, int count, ContextPack context, CancellationToken cancellationToken = default);
}
