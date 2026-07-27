using Lif.Application.Ai;
using Lif.Infrastructure.Ai.Validation;

namespace Lif.Ai.ContractTests;

/// <summary>Deterministic constraint detectors (doc 11 §6): never an AI validating an AI.</summary>
public class ConstraintCheckerTests
{
    [Fact]
    public void Check_CleanA1Sentence_HasNoViolations()
    {
        ConstraintChecker.Check("Hallo, ich heiße Anna.", TestPacks.Default()).Should().BeEmpty();
    }

    [Theory]
    [InlineData("Ich habe Deutsch gelernt.")]
    [InlineData("Sie ist nach Hause gegangen.")]
    public void Check_PerfektInText_IsDetectedWhenForbidden(string german)
    {
        var violations = ConstraintChecker.Check(german, TestPacks.Default());

        violations.Should().ContainSingle(v => v.Contains("perfekt"));
    }

    [Fact]
    public void Check_SubclauseInText_IsDetectedWhenForbidden()
    {
        var violations = ConstraintChecker.Check("Ich lerne, weil es gut ist.", TestPacks.Default());

        violations.Should().Contain(v => v.Contains("subclauses"));
    }

    [Fact]
    public void Check_ForbiddenStructureWithoutDetector_IsIgnored()
    {
        var pack = TestPacks.Default() with { ForbiddenGrammar = ["konjunktiv_ii"] };

        ConstraintChecker.Check("Hallo, ich heiße Anna.", pack).Should().BeEmpty();
    }

    [Fact]
    public void Check_SentenceOverWordLimit_IsReported()
    {
        var violations = ConstraintChecker.Check(
            "Ich gehe heute morgen ganz langsam in die neue Schule.", TestPacks.Default(maxWordsPerSentence: 8));

        violations.Should().ContainSingle(v => v.Contains("exceeds 8 words"));
    }

    [Fact]
    public void CheckExercise_ScansStemAndEveryOption()
    {
        var draft = new ExerciseDraft(
            "multiple_choice",
            "Wie heißt du?",
            ["Ich heiße Anna.", "Ich habe Anna geheißen."],
            "Ich heiße Anna.",
            null);

        var violations = ConstraintChecker.CheckExercise(draft, TestPacks.Default());

        violations.Should().ContainSingle(v => v.Contains("perfekt"), "the second option uses Perfekt");
    }
}
