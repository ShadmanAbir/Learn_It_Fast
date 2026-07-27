namespace Lif.Application.Ai;

/// <summary>
/// The ONLY student data an AI model ever sees (doc 10 §2): constraints, not authority.
/// Never mastery tables, review queues or study history (doc 11 §8).
/// </summary>
public sealed record ContextPack(
    string Level,
    string Objective,
    IReadOnlyList<string> AllowedGrammar,
    IReadOnlyList<string> ForbiddenGrammar,
    IReadOnlyList<string> VocabHints,
    IReadOnlyList<string> WeakConcepts,
    int MaxSentences = 3,
    int MaxWordsPerSentence = 8,
    string Language = "de")
{
    /// <summary>Vocab hint cap enforced at construction time (FR-AI-6, doc 10 §4).</summary>
    public const int MaxVocabHints = 40;

    /// <summary>Returns a copy whose vocab hints respect <see cref="MaxVocabHints"/> (order = relevance).</summary>
    public ContextPack Capped() => VocabHints.Count <= MaxVocabHints
        ? this
        : this with { VocabHints = VocabHints.Take(MaxVocabHints).ToList() };
}
