using Lif.Application.Ai;
using Lif.Infrastructure.Ai.Providers;

namespace Lif.Ai.ContractTests;

/// <summary>
/// Scripted provider for Ring-1 contract tests (doc 17 §3: CI never calls a live LLM).
/// Replays queued completions/exceptions and records every request it receives.
/// </summary>
public sealed class FakeAiProvider(string name = "fake") : IAiProvider
{
    private readonly Queue<Func<AiCompletion>> _script = new();

    public List<AiRequest> Requests { get; } = [];

    public string Name => name;

    public AiProviderCapabilities Capabilities { get; init; } =
        new(SizeClass: "s7b", JsonMode: true, Streaming: false, MaxContextTokens: 8192);

    public FakeAiProvider Returns(string content)
    {
        _script.Enqueue(() => new AiCompletion(content, PromptTokens: 0, CompletionTokens: 0));
        return this;
    }

    public FakeAiProvider Throws(Exception exception)
    {
        _script.Enqueue(() => throw exception);
        return this;
    }

    public Task<AiCompletion> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return _script.Count > 0
            ? Task.FromResult(_script.Dequeue()())
            : throw new InvalidOperationException($"FakeAiProvider '{name}' script is exhausted.");
    }

    public Task<ProviderHealth> ProbeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProviderHealth(true, null));
}

/// <summary>Shared fixtures for the contract tests.</summary>
public static class TestPacks
{
    /// <summary>A1.1 pack: Präsens only, Perfekt and subclauses forbidden, short sentences.</summary>
    public static ContextPack Default(int maxWordsPerSentence = 8) => new(
        Level: "A1.1",
        Objective: "Introduce yourself and greet people",
        AllowedGrammar: ["praesens", "w_questions"],
        ForbiddenGrammar: ["perfekt", "subclauses"],
        VocabHints: ["hallo", "ich", "heißen", "danke"],
        WeakConcepts: [],
        MaxWordsPerSentence: maxWordsPerSentence);

    public const string ValidExplanationJson =
        """
        {"markdown":"Use **hallo** to greet people. Say your name with *ich heiße*.",
         "examples":[{"de":"Hallo, ich heiße Anna.","en":"Hello, my name is Anna."},
                     {"de":"Guten Morgen!","en":"Good morning!"}]}
        """;

    public const string ValidExercisesJson =
        """
        {"exercises":[
          {"type":"multiple_choice","stem":"Wie heißt du?",
           "options":["Ich heiße Anna.","Danke."],"answer_key":"Ich heiße Anna.","explanation":"A name answer."},
          {"type":"multiple_choice","stem":"Guten Morgen!",
           "options":["Guten Morgen!","Nein."],"answer_key":"Guten Morgen!","explanation":null}]}
        """;
}
