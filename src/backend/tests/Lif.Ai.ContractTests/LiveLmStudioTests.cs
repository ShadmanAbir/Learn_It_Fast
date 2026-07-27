using Lif.Infrastructure.Ai.Orchestration;
using Lif.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lif.Ai.ContractTests;

/// <summary>
/// Opt-in smoke tests against a real LM Studio instance — the full pipeline
/// (PromptEngine → OpenAiCompatibleProvider → OutputValidator) with no fakes.
/// Disabled by default so CI never calls a live LLM (doc 17 §3). Enable with e.g.
///   $env:LIF_LIVE_AI = "http://192.168.0.120:1234/v1/;qwen/qwen3.5-2b"
/// </summary>
public class LiveLmStudioTests
{
    private static AiTaskOrchestrator? LiveOrchestrator(out HttpClient httpClient)
    {
        httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        var setting = Environment.GetEnvironmentVariable("LIF_LIVE_AI");
        if (string.IsNullOrWhiteSpace(setting) || setting.Split(';') is not [var baseUrl, var model])
        {
            return null;
        }

        var provider = new OpenAiCompatibleProvider(httpClient, OpenAiCompatibleOptions.LmStudio(model, baseUrl));
        return new AiTaskOrchestrator([provider], NullLogger<AiTaskOrchestrator>.Instance);
    }

    [Fact]
    public async Task Explain_AgainstLiveLmStudio_ProducesValidatedDraft()
    {
        var orchestrator = LiveOrchestrator(out var httpClient);
        using (httpClient)
        {
            if (orchestrator is null)
            {
                return; // LIF_LIVE_AI not set — stay offline.
            }

            var result = await orchestrator.ExplainAsync("hallo (greeting)", TestPacks.Default());

            result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.ToString() : string.Empty);
            result.Value.Markdown.Should().NotBeNullOrWhiteSpace();
            result.Value.Examples.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task Generate_AgainstLiveLmStudio_ProducesValidatedExercises()
    {
        var orchestrator = LiveOrchestrator(out var httpClient);
        using (httpClient)
        {
            if (orchestrator is null)
            {
                return; // LIF_LIVE_AI not set — stay offline.
            }

            var result = await orchestrator.GenerateAsync("multiple_choice", 2, TestPacks.Default());

            result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.ToString() : string.Empty);
            result.Value.Should().NotBeEmpty();
            result.Value.Should().OnlyContain(e => e.Options.Contains(e.AnswerKey));
        }
    }
}
