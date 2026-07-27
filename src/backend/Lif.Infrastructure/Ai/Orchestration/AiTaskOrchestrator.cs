using System.Globalization;
using Lif.Application.Ai;
using Lif.Infrastructure.Ai.Prompting;
using Lif.Infrastructure.Ai.Providers;
using Lif.Infrastructure.Ai.Validation;
using Lif.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace Lif.Infrastructure.Ai.Orchestration;

/// <summary>Stable error codes for the AI gateway; callers fall back to curated content on failure.</summary>
public static class AiErrors
{
    public static readonly Error NoProviderConfigured = new(
        "ai.no_provider", "No AI provider is configured.");

    public static readonly Error AllProvidersFailed = new(
        "ai.generation_failed", "All configured AI providers failed validation for this task.");
}

/// <summary>
/// The request lifecycle of doc 10 §2 (v1): prompt render → execute → validate (schema +
/// constraints) → repair-prompt retries (≤ 2) → next provider in the chain. Never plans,
/// never picks topics — it renders content for decisions already made (doc 10 §5).
/// Curated-content fallback lives with the callers, who own the curated bank.
/// </summary>
public sealed partial class AiTaskOrchestrator(
    IReadOnlyList<IAiProvider> providerChain,
    ILogger<AiTaskOrchestrator> logger) : IExplainer, IExerciseGenerator
{
    /// <summary>Repair attempts per provider after the initial call (doc 10 §2 step 7).</summary>
    public const int MaxRepairs = 2;

    public Task<Result<ExplanationDraft>> ExplainAsync(
        string conceptTitle, ContextPack context, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            TemplateCatalog.Explain,
            context,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["concept_title"] = conceptTitle },
            raw =>
            {
                var outcome = OutputValidator.ValidateExplanation(raw);
                if (!outcome.IsValid)
                {
                    return outcome;
                }

                var constraints = outcome.Value!.Examples
                    .SelectMany(e => ConstraintChecker.Check(e.De, context))
                    .ToList();
                return constraints.Count > 0
                    ? ValidationOutcome.Invalid<ExplanationDraft>([.. constraints])
                    : outcome;
            },
            cancellationToken);

    public Task<Result<IReadOnlyList<ExerciseDraft>>> GenerateAsync(
        string exerciseType, int count, ContextPack context, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            TemplateCatalog.GenExercise,
            context,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["exercise_type"] = exerciseType,
                ["count"] = count.ToString(CultureInfo.InvariantCulture),
            },
            raw =>
            {
                var outcome = OutputValidator.ValidateExercises(raw, exerciseType, count);
                if (!outcome.IsValid)
                {
                    return outcome;
                }

                var constraints = outcome.Value!
                    .SelectMany(d => ConstraintChecker.CheckExercise(d, context))
                    .ToList();
                return constraints.Count > 0
                    ? ValidationOutcome.Invalid<IReadOnlyList<ExerciseDraft>>([.. constraints])
                    : outcome;
            },
            cancellationToken);

    /// <summary>Runs one task through the provider chain with the validate→repair loop.</summary>
    private async Task<Result<T>> ExecuteAsync<T>(
        string taskType,
        ContextPack context,
        IReadOnlyDictionary<string, string> taskValues,
        Func<string, ValidationOutcome<T>> validate,
        CancellationToken cancellationToken)
        where T : class
    {
        if (providerChain.Count == 0)
        {
            return Result.Failure<T>(AiErrors.NoProviderConfigured);
        }

        foreach (var provider in providerChain)
        {
            var prompt = PromptEngine.Render(taskType, provider.Capabilities, context, taskValues);
            var user = prompt.User;

            for (var attempt = 0; attempt <= MaxRepairs; attempt++)
            {
                string raw;
                try
                {
                    raw = (await provider.CompleteAsync(
                        new AiRequest(prompt.System, user, prompt.Temperature, prompt.MaxTokens, JsonMode: true),
                        cancellationToken)).Content;
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
                {
                    LogProviderFailed(ex, provider.Name, taskType);
                    break; // Provider unreachable/broken → next provider, not another repair.
                }

                var outcome = validate(raw);
                if (outcome.IsValid)
                {
                    return outcome.Value!;
                }

                LogOutputInvalid(provider.Name, taskType, attempt + 1, string.Join(" | ", outcome.Violations));

                // Repair prompt (doc 11 §6): original ask + the exact violations, corrected JSON only.
                user = $"{prompt.User}\n\nYour last output was invalid:\n- {string.Join("\n- ", outcome.Violations)}\nOutput ONLY corrected JSON.";
            }
        }

        return Result.Failure<T>(AiErrors.AllProvidersFailed);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI provider {Provider} failed on {Task}; trying next in chain")]
    private partial void LogProviderFailed(Exception ex, string provider, string task);

    [LoggerMessage(Level = LogLevel.Information, Message = "AI output invalid ({Provider}, {Task}, attempt {Attempt}): {Violations}")]
    private partial void LogOutputInvalid(string provider, string task, int attempt, string violations);
}
