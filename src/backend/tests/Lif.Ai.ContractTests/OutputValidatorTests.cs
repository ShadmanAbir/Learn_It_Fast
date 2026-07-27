using Lif.Infrastructure.Ai.Validation;

namespace Lif.Ai.ContractTests;

/// <summary>Output-contract tests (doc 11 §6): typed parsing, never a "good enough" regex.</summary>
public class OutputValidatorTests
{
    [Fact]
    public void ValidateExplanation_WellFormed_ReturnsTypedDraft()
    {
        var outcome = OutputValidator.ValidateExplanation(TestPacks.ValidExplanationJson);

        outcome.IsValid.Should().BeTrue();
        outcome.Value!.Markdown.Should().StartWith("Use **hallo**");
        outcome.Value.Examples.Should().HaveCount(2);
        outcome.Value.Examples[0].De.Should().Be("Hallo, ich heiße Anna.");
    }

    [Fact]
    public void ValidateExplanation_NotJson_ReportsParseViolation()
    {
        var outcome = OutputValidator.ValidateExplanation("Sure! Here is the explanation you asked for:");

        outcome.IsValid.Should().BeFalse();
        outcome.Violations.Should().ContainSingle(v => v.Contains("not valid JSON"));
    }

    [Fact]
    public void ValidateExplanation_WrongExampleCount_IsRejected()
    {
        var outcome = OutputValidator.ValidateExplanation(
            """{"markdown":"Ok.","examples":[{"de":"Hallo.","en":"Hello."}]}""");

        outcome.IsValid.Should().BeFalse();
        outcome.Violations.Should().ContainSingle(v => v.Contains("exactly 2"));
    }

    [Fact]
    public void ValidateExplanation_MarkdownOver200Words_IsRejected()
    {
        var longMarkdown = string.Join(' ', Enumerable.Repeat("wort", 201));
        var outcome = OutputValidator.ValidateExplanation(
            $$"""{"markdown":"{{longMarkdown}}","examples":[{"de":"Hallo.","en":"Hi."},{"de":"Danke.","en":"Thanks."}]}""");

        outcome.IsValid.Should().BeFalse();
        outcome.Violations.Should().ContainSingle(v => v.Contains("200 words"));
    }

    [Fact]
    public void ValidateExercises_WellFormed_ReturnsAllDrafts()
    {
        var outcome = OutputValidator.ValidateExercises(TestPacks.ValidExercisesJson, "multiple_choice", 2);

        outcome.IsValid.Should().BeTrue();
        outcome.Value.Should().HaveCount(2);
        outcome.Value![0].AnswerKey.Should().Be("Ich heiße Anna.");
    }

    [Fact]
    public void ValidateExercises_WrongCount_IsRejected()
    {
        var outcome = OutputValidator.ValidateExercises(TestPacks.ValidExercisesJson, "multiple_choice", 3);

        outcome.IsValid.Should().BeFalse();
        outcome.Violations.Should().ContainSingle(v => v.Contains("exactly 3"));
    }

    [Fact]
    public void ValidateExercises_AnswerKeyNotInOptions_IsRejected()
    {
        var outcome = OutputValidator.ValidateExercises(
            """
            {"exercises":[{"type":"multiple_choice","stem":"Wie heißt du?",
             "options":["Danke.","Nein."],"answer_key":"Ich heiße Anna.","explanation":null}]}
            """,
            "multiple_choice", 1);

        outcome.IsValid.Should().BeFalse();
        outcome.Violations.Should().ContainSingle(v => v.Contains("one of 'options'"));
    }

    [Fact]
    public void ValidateExercises_WrongType_IsRejected()
    {
        var outcome = OutputValidator.ValidateExercises(
            """
            {"exercises":[{"type":"cloze","stem":"Ich ___ Anna.",
             "options":[],"answer_key":"heiße","explanation":null}]}
            """,
            "multiple_choice", 1);

        outcome.IsValid.Should().BeFalse();
        outcome.Violations.Should().Contain(v => v.Contains("'type' must be 'multiple_choice'"));
    }
}
