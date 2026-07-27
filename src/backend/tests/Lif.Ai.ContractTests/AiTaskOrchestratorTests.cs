using Lif.Infrastructure.Ai.Orchestration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lif.Ai.ContractTests;

/// <summary>
/// Lifecycle tests for doc 10 §2: render → execute → validate → repair (≤ 2) → next provider
/// → stable failure code. Providers are scripted fakes — CI never calls a live LLM (doc 17 §3).
/// </summary>
public class AiTaskOrchestratorTests
{
    private static AiTaskOrchestrator Orchestrator(params FakeAiProvider[] chain) =>
        new(chain, NullLogger<AiTaskOrchestrator>.Instance);

    [Fact]
    public async Task EmptyChain_FailsWithNoProviderCode()
    {
        var result = await Orchestrator().ExplainAsync("Begrüßung", TestPacks.Default());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AiErrors.NoProviderConfigured);
    }

    [Fact]
    public async Task ValidFirstOutput_SucceedsWithOneJsonModeCall()
    {
        var provider = new FakeAiProvider().Returns(TestPacks.ValidExplanationJson);

        var result = await Orchestrator(provider).ExplainAsync("Begrüßung", TestPacks.Default());

        result.IsSuccess.Should().BeTrue();
        result.Value.Examples.Should().HaveCount(2);
        provider.Requests.Should().ContainSingle().Which.JsonMode.Should().BeTrue();
    }

    [Fact]
    public async Task InvalidOutput_TriggersRepairPromptWithTheExactViolations()
    {
        var provider = new FakeAiProvider()
            .Returns("not json at all")
            .Returns(TestPacks.ValidExplanationJson);

        var result = await Orchestrator(provider).ExplainAsync("Begrüßung", TestPacks.Default());

        result.IsSuccess.Should().BeTrue();
        provider.Requests.Should().HaveCount(2);
        provider.Requests[1].UserPrompt.Should()
            .Contain("Your last output was invalid")
            .And.Contain("not valid JSON")
            .And.Contain(provider.Requests[0].UserPrompt, "the repair keeps the original ask");
    }

    [Fact]
    public async Task ConstraintViolation_AlsoCountsAsInvalid_AndIsRepaired()
    {
        var perfektExplanation =
            """
            {"markdown":"Ok.","examples":[{"de":"Ich habe Deutsch gelernt.","en":"I learned German."},
             {"de":"Hallo!","en":"Hello!"}]}
            """;
        var provider = new FakeAiProvider()
            .Returns(perfektExplanation)
            .Returns(TestPacks.ValidExplanationJson);

        var result = await Orchestrator(provider).ExplainAsync("Begrüßung", TestPacks.Default());

        result.IsSuccess.Should().BeTrue();
        provider.Requests.Should().HaveCount(2);
        provider.Requests[1].UserPrompt.Should().Contain("perfekt");
    }

    [Fact]
    public async Task PersistentlyInvalidProvider_GetsExactlyOneCallPlusMaxRepairs()
    {
        var provider = new FakeAiProvider()
            .Returns("junk 1").Returns("junk 2").Returns("junk 3");

        var result = await Orchestrator(provider).ExplainAsync("Begrüßung", TestPacks.Default());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AiErrors.AllProvidersFailed);
        provider.Requests.Should().HaveCount(1 + AiTaskOrchestrator.MaxRepairs);
    }

    [Fact]
    public async Task UnreachableProvider_FailsOverToNextInChain_WithoutBurningRepairs()
    {
        var down = new FakeAiProvider("lmstudio").Throws(new HttpRequestException("connection refused"));
        var up = new FakeAiProvider("ollama").Returns(TestPacks.ValidExercisesJson);

        var result = await Orchestrator(down, up)
            .GenerateAsync("multiple_choice", 2, TestPacks.Default());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        down.Requests.Should().ContainSingle("a dead provider is skipped, not repaired");
        up.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task WholeChainExhausted_FailsWithGenerationFailedCode()
    {
        var first = new FakeAiProvider("lmstudio").Returns("junk").Returns("junk").Returns("junk");
        var second = new FakeAiProvider("ollama").Throws(new HttpRequestException("down"));

        var result = await Orchestrator(first, second)
            .GenerateAsync("multiple_choice", 2, TestPacks.Default());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AiErrors.AllProvidersFailed);
    }
}
