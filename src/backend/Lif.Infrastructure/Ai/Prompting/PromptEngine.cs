using System.Text.RegularExpressions;
using Lif.Application.Ai;
using Lif.Infrastructure.Ai.Providers;

namespace Lif.Infrastructure.Ai.Prompting;

/// <summary>A fully rendered, budget-checked prompt ready for a provider call.</summary>
public sealed record RenderedPrompt(
    string System,
    string User,
    double Temperature,
    int MaxTokens,
    string TemplateVersion);

/// <summary>
/// Strict placeholder substitution (doc 11 §2): a missing key throws — a prompt with silent
/// blanks is worse than no prompt at all. Unused-value checking is opt-in: the engine binds a
/// shared base set and not every template consumes every base key.
/// </summary>
public static partial class PlaceholderBinder
{
    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex Placeholder();

    public static string Bind(
        string skeleton, IReadOnlyDictionary<string, string> values, bool requireAllValuesUsed = false)
    {
        var unused = new HashSet<string>(values.Keys, StringComparer.Ordinal);
        var bound = Placeholder().Replace(skeleton, match =>
        {
            var key = match.Groups[1].Value;
            if (!values.TryGetValue(key, out var value))
            {
                throw new PromptRenderException($"Placeholder '{{{{{key}}}}}' has no bound value.");
            }

            unused.Remove(key);
            return value;
        });

        return requireAllValuesUsed && unused.Count > 0
            ? throw new PromptRenderException($"Bound values never used by the template: {string.Join(", ", unused)}.")
            : bound;
    }
}

/// <summary>
/// Deterministic prompt budget enforcement (FR-AI-6, doc 11 §2): optional context shrinks
/// first (vocab hints), rules and the output skeleton are never trimmed.
/// </summary>
public static class BudgetTrimmer
{
    /// <summary>Cheap token estimate (≈4 chars/token) — a guardrail, not an exact count.</summary>
    public static int EstimateTokens(string text) => (text.Length + 3) / 4;

    /// <summary>Trims the vocab hint list until the rendered prompt fits the context budget.</summary>
    public static IReadOnlyList<string> FitVocabHints(
        IReadOnlyList<string> hints, string promptWithoutHints, int maxContextTokens)
    {
        var kept = new List<string>(hints);
        while (kept.Count > 0 &&
               EstimateTokens(promptWithoutHints) + EstimateTokens(string.Join(", ", kept)) > maxContextTokens)
        {
            kept.RemoveAt(kept.Count - 1); // Hints arrive relevance-ordered; drop the tail first.
        }

        return kept;
    }
}

/// <summary>
/// PromptRequest → RenderedPrompt (doc 11 §2): template select by (task, size class),
/// strict binding from the ContextPack, budget trim. Engines never concatenate prompt strings.
/// </summary>
public static class PromptEngine
{
    public static RenderedPrompt Render(
        string taskType,
        AiProviderCapabilities capabilities,
        ContextPack context,
        IReadOnlyDictionary<string, string> taskValues)
    {
        var template = TemplateCatalog.GetActive(taskType, capabilities.SizeClass);
        var pack = context.Capped();

        var values = new Dictionary<string, string>(taskValues, StringComparer.Ordinal)
        {
            ["cefr"] = pack.Level,
            ["objective"] = pack.Objective,
            ["allowed_grammar"] = string.Join(", ", pack.AllowedGrammar),
            ["forbidden_grammar"] = string.Join(", ", pack.ForbiddenGrammar),
            ["max_words_per_sentence"] = pack.MaxWordsPerSentence.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        if (template.UserSkeleton.Contains("{{vocab_hints}}", StringComparison.Ordinal))
        {
            var withoutHints = PlaceholderBinder.Bind(
                template.UserSkeleton, new Dictionary<string, string>(values) { ["vocab_hints"] = string.Empty });
            var hints = BudgetTrimmer.FitVocabHints(pack.VocabHints, withoutHints, capabilities.MaxContextTokens);
            values["vocab_hints"] = string.Join(", ", hints);
        }

        var user = PlaceholderBinder.Bind(template.UserSkeleton, values);
        return new RenderedPrompt(template.System, user, template.Temperature, template.MaxTokens, template.Version);
    }
}
