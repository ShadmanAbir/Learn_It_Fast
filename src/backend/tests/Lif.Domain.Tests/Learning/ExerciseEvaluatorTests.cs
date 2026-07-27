using Lif.Domain.Learning;
using Lif.SharedKernel.Primitives;

namespace Lif.Domain.Tests.Learning;

public class ExerciseEvaluatorTests
{
    private static ExerciseSpec Spec(ExerciseType type, string answerKey, IReadOnlyList<string>? options = null) =>
        new(type, "Stem", options ?? [], answerKey, [Guid.CreateVersion7()], Difficulty.From(2));

    [Theory]
    [InlineData("der Tisch", "der Tisch", 1.0)]
    [InlineData("der Tisch", "  der   Tisch  ", 1.0)] // whitespace normalized
    [InlineData("der Tisch", "die Tisch", 0.0)]
    [InlineData("der Tisch", "der tisch", 0.0)] // MC is exact — the option was visible
    public void MultipleChoice_ScoresExactMatchOnly(string answerKey, string response, double expected)
    {
        var spec = Spec(ExerciseType.MultipleChoice, answerKey, ["der Tisch", "die Tisch", "das Tisch"]);

        ExerciseEvaluator.Evaluate(spec, response).Outcome.Should().BeApproximately(expected, 1e-12);
    }

    [Theory]
    [InlineData("Tisch", "Tisch", 1.0, EvaluationResult.Correct)]
    [InlineData("Tisch", "tisch", 0.5, EvaluationResult.PartiallyCorrect)] // German noun capitalization
    [InlineData("Tisch", "Stuhl", 0.0, EvaluationResult.Incorrect)]
    public void Cloze_GivesHalfCreditForCaseOnlyMismatch(string answerKey, string response, double expected, string verdict)
    {
        var result = ExerciseEvaluator.Evaluate(Spec(ExerciseType.Cloze, answerKey), response);

        result.Outcome.Should().BeApproximately(expected, 1e-12);
        result.Verdict.Should().Be(verdict);
    }

    [Fact]
    public void Matching_ScoresFractionOfCorrectPairs()
    {
        var spec = Spec(ExerciseType.Matching, "Hund=dog|Katze=cat|Vogel=bird");

        var all = ExerciseEvaluator.Evaluate(spec, "Hund=dog|Katze=cat|Vogel=bird");
        var twoOfThree = ExerciseEvaluator.Evaluate(spec, "Hund=dog|Katze=bird|Vogel=bird");
        var none = ExerciseEvaluator.Evaluate(spec, "Hund=cat|Katze=bird|Vogel=dog");

        all.Outcome.Should().BeApproximately(1.0, 1e-12);
        twoOfThree.Outcome.Should().BeApproximately(2.0 / 3.0, 1e-12);
        none.Outcome.Should().BeApproximately(0.0, 1e-12);
    }

    [Fact]
    public void Matching_PairOrderDoesNotMatter()
    {
        var spec = Spec(ExerciseType.Matching, "Hund=dog|Katze=cat");

        ExerciseEvaluator.Evaluate(spec, "Katze=cat|Hund=dog").Outcome.Should().BeApproximately(1.0, 1e-12);
    }

    [Fact]
    public void Ordering_ExactSequenceIsFullyCorrect()
    {
        var spec = Spec(ExerciseType.Ordering, "Ich|gehe|nach|Hause");

        ExerciseEvaluator.Evaluate(spec, "Ich|gehe|nach|Hause").Outcome.Should().BeApproximately(1.0, 1e-12);
    }

    [Fact]
    public void Ordering_PartialCredit_IsCappedBelowCorrect()
    {
        var spec = Spec(ExerciseType.Ordering, "Ich|gehe|nach|Hause");

        // 3 of 4 positions right → raw 0.75, still capped at 0.75 (< 1.0).
        var result = ExerciseEvaluator.Evaluate(spec, "Ich|gehe|Hause|nach");

        result.Outcome.Should().BeApproximately(0.5, 1e-12); // positions 0 and 1 correct
        result.Verdict.Should().Be(EvaluationResult.PartiallyCorrect);
    }

    [Fact]
    public void Ordering_NearMiss_NeverReachesFullCredit()
    {
        var spec = Spec(ExerciseType.Ordering, "a|b|c|d|e|f|g|h");

        // 7 of 8 in place → raw 0.875, capped at 0.75.
        var result = ExerciseEvaluator.Evaluate(spec, "a|b|c|d|e|f|h|g");

        result.Outcome.Should().BeApproximately(0.75, 1e-12);
    }

    [Fact]
    public void Ordering_WrongLengthResponse_ScoresOnMatchingPrefixPositionsOnly()
    {
        var spec = Spec(ExerciseType.Ordering, "a|b|c|d");

        ExerciseEvaluator.Evaluate(spec, "a|b").Outcome.Should().BeApproximately(0.5, 1e-12);
    }

    [Fact]
    public void EmptyResponse_IsIncorrect_NeverThrows()
    {
        foreach (var type in new[] { ExerciseType.MultipleChoice, ExerciseType.Cloze, ExerciseType.Matching, ExerciseType.Ordering })
        {
            var result = ExerciseEvaluator.Evaluate(Spec(type, "Antwort"), string.Empty);
            result.Outcome.Should().Be(0.0);
            result.Verdict.Should().Be(EvaluationResult.Incorrect);
        }
    }
}
