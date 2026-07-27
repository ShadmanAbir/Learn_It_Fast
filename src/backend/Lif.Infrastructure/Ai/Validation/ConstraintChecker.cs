using System.Text.RegularExpressions;
using Lif.Application.Ai;

namespace Lif.Infrastructure.Ai.Validation;

/// <summary>
/// Deterministic .NET checks on generated German content (doc 11 §6): forbidden-structure
/// detectors and length limits. Never another AI call — an AI validating an AI would
/// compound unreliability at small scales.
/// </summary>
public static partial class ConstraintChecker
{
    [GeneratedRegex(@"\b(habe|hast|hat|haben|habt|bin|bist|ist|sind|seid)\b[^.!?]*\bge\w{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex PerfektDetector();

    [GeneratedRegex(@"\b(weil|dass|obwohl|wenn)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SubclauseDetector();

    [GeneratedRegex(@"[^.!?]+[.!?]?")]
    private static partial Regex SentenceSplitter();

    /// <summary>Grammar-code → detector; only structures with a reliable surface pattern are checked.</summary>
    private static readonly Dictionary<string, Regex> Detectors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["perfekt"] = PerfektDetector(),
        ["subclauses"] = SubclauseDetector(),
    };

    /// <summary>Checks one piece of learner-facing German against the pack's constraints.</summary>
    public static IReadOnlyList<string> Check(string germanText, ContextPack context)
    {
        var violations = new List<string>();

        foreach (var forbidden in context.ForbiddenGrammar)
        {
            if (Detectors.TryGetValue(forbidden, out var detector) && detector.IsMatch(germanText))
            {
                violations.Add($"Forbidden structure '{forbidden}' detected.");
            }
        }

        foreach (var sentence in SentenceSplitter().Matches(germanText).Select(m => m.Value.Trim()))
        {
            var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (words > context.MaxWordsPerSentence)
            {
                violations.Add($"Sentence exceeds {context.MaxWordsPerSentence} words: \"{sentence}\"");
            }
        }

        return violations;
    }

    /// <summary>Applies <see cref="Check"/> to every learner-facing string of an exercise draft.</summary>
    public static IReadOnlyList<string> CheckExercise(ExerciseDraft draft, ContextPack context) =>
        [.. Check(draft.Stem, context), .. draft.Options.SelectMany(o => Check(o, context))];
}
