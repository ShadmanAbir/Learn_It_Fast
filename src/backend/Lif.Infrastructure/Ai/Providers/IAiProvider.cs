namespace Lif.Infrastructure.Ai.Providers;

/// <summary>What a configured provider/model can do; drives routing and prompt size class (doc 10 §1).</summary>
public sealed record AiProviderCapabilities(
    string SizeClass,
    bool JsonMode,
    bool Streaming,
    int MaxContextTokens);

/// <summary>One chat completion request; the only shape the provider tier understands.</summary>
public sealed record AiRequest(
    string SystemPrompt,
    string UserPrompt,
    double Temperature,
    int MaxTokens,
    bool JsonMode = false);

/// <summary>Raw completion; validation happens above this tier, never inside providers.</summary>
public sealed record AiCompletion(string Content, int PromptTokens, int CompletionTokens);

/// <summary>Probe result for the setup wizard and readiness checks (doc 10 §1).</summary>
public sealed record ProviderHealth(bool IsHealthy, string? Detail);

/// <summary>
/// Provider port (doc 10 §1 tier 2): one method surface over LM Studio / Ollama / OpenAI-class
/// backends. No pedagogy, no prompts, no validation — chat completion only.
/// </summary>
public interface IAiProvider
{
    /// <summary>Stable name used in routing chains and call logs ("lmstudio", "ollama", "openai").</summary>
    public string Name { get; }

    public AiProviderCapabilities Capabilities { get; }

    public Task<AiCompletion> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default);

    public Task<ProviderHealth> ProbeAsync(CancellationToken cancellationToken = default);
}
