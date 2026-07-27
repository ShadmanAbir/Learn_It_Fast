using Lif.Infrastructure.Ai.Prompting;
using Lif.Infrastructure.Ai.Providers;

namespace Lif.Ai.ContractTests;

/// <summary>Ring-1 prompt contract tests (doc 11 §2): strict binding, budget trim, catalog fallback.</summary>
public class PromptEngineTests
{
    private static readonly AiProviderCapabilities S7b = new("s7b", JsonMode: true, Streaming: false, MaxContextTokens: 8192);

    [Fact]
    public void Render_Explain_BindsAllPlaceholdersFromPackAndTaskValues()
    {
        var prompt = PromptEngine.Render(
            TemplateCatalog.Explain, S7b, TestPacks.Default(),
            new Dictionary<string, string> { ["concept_title"] = "W-Fragen" });

        prompt.User.Should().Contain("W-Fragen")
            .And.Contain("A1.1")
            .And.Contain("praesens, w_questions")
            .And.Contain("perfekt, subclauses")
            .And.NotContain("{{", "a rendered prompt must have no unbound placeholders");
        prompt.TemplateVersion.Should().Be("1.0.0");
        prompt.System.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Render_MissingTaskValue_ThrowsInsteadOfLeavingBlanks()
    {
        var act = () => PromptEngine.Render(
            TemplateCatalog.Explain, S7b, TestPacks.Default(), new Dictionary<string, string>());

        act.Should().Throw<PromptRenderException>().WithMessage("*concept_title*");
    }

    [Fact]
    public void Render_TinyBudget_TrimsVocabHintsTailFirst_ButKeepsRules()
    {
        var hints = Enumerable.Range(1, 30).Select(i => $"wort{i:d2}").ToList();
        var pack = TestPacks.Default() with { VocabHints = hints };
        var tiny = S7b with { MaxContextTokens = 150 };

        var prompt = PromptEngine.Render(
            TemplateCatalog.GenExercise, tiny, pack,
            new Dictionary<string, string> { ["exercise_type"] = "multiple_choice", ["count"] = "2" });

        prompt.User.Should().Contain("wort01", "the most relevant hints survive the trim");
        prompt.User.Should().NotContain("wort30", "hints are relevance-ordered; the tail goes first");
        prompt.User.Should().Contain("Do NOT use", "rules are never trimmed");
    }

    [Fact]
    public void Binder_RequireAllValuesUsed_FlagsDeadBindings()
    {
        var act = () => PlaceholderBinder.Bind(
            "Hello {{name}}",
            new Dictionary<string, string> { ["name"] = "Anna", ["unused"] = "x" },
            requireAllValuesUsed: true);

        act.Should().Throw<PromptRenderException>().WithMessage("*unused*");
    }

    [Fact]
    public void Catalog_UnknownSizeClass_FallsBackToS7b()
    {
        var template = TemplateCatalog.GetActive(TemplateCatalog.Explain, "cloud");

        template.SizeClass.Should().Be("s7b");
    }

    [Fact]
    public void Catalog_UnknownTask_Throws()
    {
        var act = () => TemplateCatalog.GetActive("write_poem", "s7b");

        act.Should().Throw<PromptRenderException>();
    }
}
