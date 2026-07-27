namespace Lif.Domain.Learning;

/// <summary>Outcome of a deterministic evaluation.</summary>
public sealed record EvaluationResult(double Outcome, string Verdict)
{
    public const string Correct = "correct";
    public const string PartiallyCorrect = "partially_correct";
    public const string Incorrect = "incorrect";
}

/// <summary>
/// Pure, deterministic scoring for the Phase 1 exercise types. German is case-sensitive
/// (noun capitalization), so a case-only mismatch earns partial credit instead of full.
/// </summary>
public static class ExerciseEvaluator
{
    public static EvaluationResult Evaluate(ExerciseSpec spec, string response)
    {
        var outcome = spec.Type switch
        {
            ExerciseType.MultipleChoice => ScoreExact(spec.AnswerKey, response),
            ExerciseType.Cloze => ScoreCloze(spec.AnswerKey, response),
            ExerciseType.Matching => ScorePairs(spec.AnswerKey, response),
            ExerciseType.Ordering => ScoreSequence(spec.AnswerKey, response),
            _ => 0.0,
        };

        var verdict = outcome switch
        {
            >= 1.0 => EvaluationResult.Correct,
            > 0.0 => EvaluationResult.PartiallyCorrect,
            _ => EvaluationResult.Incorrect,
        };

        return new EvaluationResult(outcome, verdict);
    }

    private static double ScoreExact(string answerKey, string response) =>
        Normalize(response).Equals(Normalize(answerKey), StringComparison.Ordinal) ? 1.0 : 0.0;

    private static double ScoreCloze(string answerKey, string response)
    {
        var expected = Normalize(answerKey);
        var actual = Normalize(response);
        if (actual.Equals(expected, StringComparison.Ordinal))
        {
            return 1.0;
        }

        // Right word, wrong capitalization — half credit (German nouns!).
        return actual.Equals(expected, StringComparison.OrdinalIgnoreCase) ? 0.5 : 0.0;
    }

    private static double ScorePairs(string answerKey, string response)
    {
        var expected = ParsePairs(answerKey);
        if (expected.Count == 0)
        {
            return 0.0;
        }

        var actual = ParsePairs(response);
        var correct = expected.Count(pair =>
            actual.TryGetValue(pair.Key, out var value) &&
            value.Equals(pair.Value, StringComparison.Ordinal));

        return (double)correct / expected.Count;
    }

    private static double ScoreSequence(string answerKey, string response)
    {
        var expected = SplitTokens(answerKey);
        if (expected.Length == 0)
        {
            return 0.0;
        }

        var actual = SplitTokens(response);
        if (actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            return 1.0;
        }

        // Partial credit: fraction of positions holding the right token.
        var correct = expected
            .Select((token, i) => i < actual.Length && actual[i].Equals(token, StringComparison.Ordinal))
            .Count(match => match);

        // An almost-right sentence is not "correct" — cap partial credit below 1.
        return Math.Min(0.75, (double)correct / expected.Length);
    }

    private static Dictionary<string, string> ParsePairs(string encoded) =>
        SplitTokens(encoded)
            .Select(pair => pair.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .GroupBy(parts => Normalize(parts[0]))
            .ToDictionary(g => g.Key, g => Normalize(g.First()[1]), StringComparer.Ordinal);

    private static string[] SplitTokens(string encoded) =>
        encoded.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string Normalize(string text) =>
        string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
}
