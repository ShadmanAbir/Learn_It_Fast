namespace Lif.Infrastructure.Ai.Prompting;

/// <summary>
/// A versioned prompt artifact (doc 11 §1): never a string literal in engine code.
/// v1 ships an in-code catalog; the ai.prompt_templates table takes over in a later phase.
/// </summary>
public sealed record PromptTemplate(
    string TaskType,
    string SizeClass,
    string Version,
    string System,
    string UserSkeleton,
    double Temperature,
    int MaxTokens);

/// <summary>
/// In-memory template catalog for the v1 tasks. Instructions in English, content in German
/// (doc 11 §1 rule 5); constraints are phrased as rules, never as questions (rule 4).
/// </summary>
public static class TemplateCatalog
{
    public const string Explain = "explain";
    public const string GenExercise = "gen_exercise";

    private static readonly PromptTemplate[] Templates =
    [
        new(Explain, "s7b", "1.0.0",
            "You are a German language tutor. Follow ALL rules exactly. Output JSON only.",
            """
            TASK: Explain the German concept "{{concept_title}}" to a student.
            STUDENT LEVEL: {{cefr}}
            OBJECTIVE: {{objective}}
            RULES:
            - Use ONLY these grammar structures: {{allowed_grammar}}
            - Do NOT use: {{forbidden_grammar}}
            - Markdown body: 200 words maximum, English meta-language, German examples.
            - Include exactly 2 German example sentences with English gloss.
            OUTPUT: JSON only, matching exactly:
            {"markdown":"...","examples":[{"de":"...","en":"..."},{"de":"...","en":"..."}]}
            """,
            Temperature: 0.3, MaxTokens: 700),
        new(GenExercise, "s7b", "1.0.0",
            "You are a German language exercise generator. Follow ALL rules exactly. Output JSON only.",
            """
            TASK: Create {{count}} {{exercise_type}} exercises.
            TARGET: {{objective}}
            STUDENT LEVEL: {{cefr}}
            RULES:
            - Use ONLY these grammar structures: {{allowed_grammar}}
            - Do NOT use: {{forbidden_grammar}}
            - Prefer these words: {{vocab_hints}}
            - Sentences: {{max_words_per_sentence}} words maximum.
            - Every answer_key must be exact; for multiple_choice it must be one of the options.
            OUTPUT: JSON only, matching exactly:
            {"exercises":[{"type":"{{exercise_type}}","stem":"...","options":["..."],"answer_key":"...","explanation":"..."}]}
            """,
            Temperature: 0.3, MaxTokens: 900),
    ];

    /// <summary>Active template for (taskType, sizeClass); falls back to s7b when the class has no variant.</summary>
    public static PromptTemplate GetActive(string taskType, string sizeClass)
    {
        var exact = Templates.FirstOrDefault(t => t.TaskType == taskType && t.SizeClass == sizeClass);
        var template = exact ?? Templates.FirstOrDefault(t => t.TaskType == taskType && t.SizeClass == "s7b");
        return template ?? throw new PromptRenderException($"No template registered for task '{taskType}'.");
    }
}

/// <summary>Raised when rendering breaks a prompt contract; callers treat it as a bug, not a retry.</summary>
public sealed class PromptRenderException(string message) : Exception(message);
