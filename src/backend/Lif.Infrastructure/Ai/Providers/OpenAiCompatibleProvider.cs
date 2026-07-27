using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lif.Infrastructure.Ai.Providers;

/// <summary>Configuration for one OpenAI-compatible backend (doc 10 §1 provider list).</summary>
public sealed record OpenAiCompatibleOptions(
    string Name,
    Uri BaseAddress,
    string Model,
    string? ApiKey,
    AiProviderCapabilities Capabilities,
    bool JsonViaSchema = false)
{
    /// <summary>
    /// LM Studio local server default. Current builds reject response_format "json_object"
    /// (only "json_schema" or "text"), so JSON mode goes through a permissive schema.
    /// </summary>
    public static OpenAiCompatibleOptions LmStudio(string model, string baseUrl = "http://localhost:1234/v1/") =>
        new("lmstudio", new Uri(baseUrl), model, ApiKey: null,
            new AiProviderCapabilities("s7b", JsonMode: true, Streaming: true, MaxContextTokens: 8192),
            JsonViaSchema: true);

    /// <summary>Ollama's OpenAI-compatible endpoint.</summary>
    public static OpenAiCompatibleOptions Ollama(string model, string baseUrl = "http://localhost:11434/v1/") =>
        new("ollama", new Uri(baseUrl), model, ApiKey: null,
            new AiProviderCapabilities("s7b", JsonMode: true, Streaming: true, MaxContextTokens: 8192));

    /// <summary>OpenAI cloud.</summary>
    public static OpenAiCompatibleOptions OpenAi(string model, string apiKey, string baseUrl = "https://api.openai.com/v1/") =>
        new("openai", new Uri(baseUrl), model, apiKey,
            new AiProviderCapabilities("cloud", JsonMode: true, Streaming: true, MaxContextTokens: 128000));
}

/// <summary>
/// One adapter for every /v1/chat/completions backend: LM Studio, Ollama and OpenAI differ
/// only in base address, model name, API key and capability profile (doc 10 §1).
/// </summary>
public sealed class OpenAiCompatibleProvider(HttpClient httpClient, OpenAiCompatibleOptions options) : IAiProvider
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly ResponseFormat JsonObjectFormat = new("json_object");

    // Permissive object schema: the real contract is enforced by the OutputValidator (doc 10 §4).
    private static readonly ResponseFormat JsonSchemaFormat = new(
        "json_schema",
        new JsonSchemaSpec("lif_output", JsonDocument.Parse("""{"type":"object"}""").RootElement));

    public string Name => options.Name;

    public AiProviderCapabilities Capabilities => options.Capabilities;

    public async Task<AiCompletion> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(options.BaseAddress, "chat/completions"))
        {
            Content = JsonContent.Create(new ChatRequest(
                options.Model,
                [new ChatMessage("system", request.SystemPrompt), new ChatMessage("user", request.UserPrompt)],
                request.Temperature,
                request.MaxTokens,
                request.JsonMode && Capabilities.JsonMode
                    ? options.JsonViaSchema ? JsonSchemaFormat : JsonObjectFormat
                    : null), options: Json),
        };
        if (options.ApiKey is not null)
        {
            message.Headers.Authorization = new("Bearer", options.ApiKey);
        }

        using var response = await httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var chat = await response.Content.ReadFromJsonAsync<ChatResponse>(Json, cancellationToken)
            ?? throw new InvalidOperationException($"{Name}: empty completion response.");
        var content = chat.Choices is [{ Message.Content: { } text }, ..]
            ? text
            : throw new InvalidOperationException($"{Name}: completion had no choices.");

        return new AiCompletion(content, chat.Usage?.PromptTokens ?? 0, chat.Usage?.CompletionTokens ?? 0);
    }

    public async Task<ProviderHealth> ProbeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, new Uri(options.BaseAddress, "models"));
            if (options.ApiKey is not null)
            {
                message.Headers.Authorization = new("Bearer", options.ApiKey);
            }

            using var response = await httpClient.SendAsync(message, cancellationToken);
            return response.IsSuccessStatusCode
                ? new ProviderHealth(true, null)
                : new ProviderHealth(false, $"{Name}: /models returned {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return new ProviderHealth(false, $"{Name}: {ex.Message}");
        }
    }

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ResponseFormat(
        string Type,
        [property: JsonPropertyName("json_schema")] JsonSchemaSpec? JsonSchema = null);

    private sealed record JsonSchemaSpec(string Name, JsonElement Schema);

    private sealed record ChatRequest(
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("response_format")] ResponseFormat? ResponseFormat);

    private sealed record ChatChoice(ChatMessage Message);

    private sealed record ChatUsage(
        [property: JsonPropertyName("prompt_tokens")] int PromptTokens,
        [property: JsonPropertyName("completion_tokens")] int CompletionTokens);

    private sealed record ChatResponse(IReadOnlyList<ChatChoice>? Choices, ChatUsage? Usage);
}
