using System.Text.Json;
using System.Text.Json.Serialization;
using Lif.Application.Ai;

namespace Lif.Infrastructure.Ai.Validation;

/// <summary>
/// Parses raw model output against the task's output contract (doc 11 §6). Returns the typed
/// draft or a violation list that feeds the repair prompt — never a regex "good enough" hack.
/// </summary>
public static class OutputValidator
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    /// <summary>explanation.v1: markdown ≤ 200 words + exactly 2 glossed examples.</summary>
    public static ValidationOutcome<ExplanationDraft> ValidateExplanation(string raw)
    {
        if (!TryParse<ExplanationPayload>(raw, out var payload, out var parseError))
        {
            return ValidationOutcome.Invalid<ExplanationDraft>(parseError);
        }

        var violations = new List<string>();
        if (string.IsNullOrWhiteSpace(payload.Markdown))
        {
            violations.Add("'markdown' is missing or empty.");
        }
        else if (CountWords(payload.Markdown) > 200)
        {
            violations.Add("'markdown' exceeds 200 words.");
        }

        var examples = payload.Examples ?? [];
        if (examples.Count != 2 || examples.Any(e => string.IsNullOrWhiteSpace(e.De) || string.IsNullOrWhiteSpace(e.En)))
        {
            violations.Add("'examples' must contain exactly 2 items, each with non-empty 'de' and 'en'.");
        }

        return violations.Count > 0
            ? ValidationOutcome.Invalid<ExplanationDraft>([.. violations])
            : ValidationOutcome.Valid<ExplanationDraft>(new ExplanationDraft(
                payload.Markdown!.Trim(),
                examples.Select(e => new ExampleSentence(e.De!.Trim(), e.En!.Trim())).ToList()));
    }

    /// <summary>exercise.&lt;type&gt;.v1: requested count, non-empty stem/answer, MC answer ∈ options.</summary>
    public static ValidationOutcome<IReadOnlyList<ExerciseDraft>> ValidateExercises(
        string raw, string expectedType, int expectedCount)
    {
        if (!TryParse<ExerciseBatchPayload>(raw, out var payload, out var parseError))
        {
            return ValidationOutcome.Invalid<IReadOnlyList<ExerciseDraft>>(parseError);
        }

        var exercises = payload.Exercises ?? [];
        var violations = new List<string>();
        if (exercises.Count != expectedCount)
        {
            violations.Add($"'exercises' must contain exactly {expectedCount} items, found {exercises.Count}.");
        }

        foreach (var (exercise, index) in exercises.Select((e, i) => (e, i)))
        {
            if (string.IsNullOrWhiteSpace(exercise.Stem) || string.IsNullOrWhiteSpace(exercise.AnswerKey))
            {
                violations.Add($"exercises[{index}]: 'stem' and 'answer_key' must be non-empty.");
                continue;
            }

            if (exercise.Type != expectedType)
            {
                violations.Add($"exercises[{index}]: 'type' must be '{expectedType}'.");
            }

            if (expectedType == "multiple_choice" && !(exercise.Options ?? []).Contains(exercise.AnswerKey))
            {
                violations.Add($"exercises[{index}]: 'answer_key' must be one of 'options'.");
            }
        }

        return violations.Count > 0
            ? ValidationOutcome.Invalid<IReadOnlyList<ExerciseDraft>>([.. violations])
            : ValidationOutcome.Valid<IReadOnlyList<ExerciseDraft>>(exercises
                .Select(e => new ExerciseDraft(
                    e.Type!, e.Stem!.Trim(), e.Options ?? [], e.AnswerKey!.Trim(), e.Explanation))
                .ToList());
    }

    private static bool TryParse<T>(string raw, out T payload, out string[] error)
        where T : class
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<T>(raw, Json);
            if (parsed is not null)
            {
                payload = parsed;
                error = [];
                return true;
            }

            error = ["Output was JSON 'null'."];
        }
        catch (JsonException ex)
        {
            error = [$"Output is not valid JSON: {ex.Message}"];
        }

        payload = null!;
        return false;
    }

    private static int CountWords(string text) =>
        text.Split([' ', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;

    private sealed record ExamplePayload(string? De, string? En);

    private sealed record ExplanationPayload(string? Markdown, IReadOnlyList<ExamplePayload>? Examples);

    private sealed record ExercisePayload(
        string? Type,
        string? Stem,
        IReadOnlyList<string>? Options,
        [property: JsonPropertyName("answer_key")] string? AnswerKey,
        string? Explanation);

    private sealed record ExerciseBatchPayload(IReadOnlyList<ExercisePayload>? Exercises);
}

/// <summary>Valid draft or the violation list that seeds the repair prompt.</summary>
public sealed record ValidationOutcome<T>(T? Value, IReadOnlyList<string> Violations)
    where T : class
{
    public bool IsValid => Violations.Count == 0;
}

/// <summary>Factories live off the generic type (CA1000) so call sites can infer.</summary>
public static class ValidationOutcome
{
    public static ValidationOutcome<T> Valid<T>(T value)
        where T : class => new(value, []);

    public static ValidationOutcome<T> Invalid<T>(params string[] violations)
        where T : class => new(null, violations);
}
