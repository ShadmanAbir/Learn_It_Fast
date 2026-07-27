using System.Text.Json;

namespace Lif.Cli;

internal sealed record SeedDependency(string Prerequisite, string Type, double? MinMastery);

internal sealed record SeedConcept(
    string File,
    string Code,
    string Kind,
    string Level,
    int Difficulty,
    double MasteryThreshold,
    double Importance,
    int EstimatedMinutes,
    string Unit,
    IReadOnlyList<SeedDependency> Dependencies,
    IReadOnlyList<string> MistakeErrorCodes,
    IReadOnlyList<string> RequiredVocabulary);

internal sealed record SeedLesson(string Title, IReadOnlyList<string> Concepts);

internal sealed record SeedUnit(string File, string Code, string Level, int OrderIndex, IReadOnlyList<SeedLesson> Lessons);

internal sealed record SeedExercise(
    string File,
    string Code,
    string Type,
    IReadOnlyList<string> Concepts,
    int Difficulty,
    string Stem,
    IReadOnlyList<string> Options,
    string AnswerKey,
    string? ErrorCode);

internal sealed record SeedLexEntry(
    string File,
    string Lemma,
    string Pos,
    string? Gender,
    bool? Separable,
    int Difficulty,
    int? FrequencyRank,
    int SenseCount);

internal sealed record SeedBlueprintSection(string Skill, int ItemCount, double PassPercentage);

internal sealed record SeedBlueprint(string File, string Kind, string Level, string? Unit, IReadOnlyList<SeedBlueprintSection> Sections);

/// <summary>Parsed seed package (ADR-014 layout). Parsing errors land in the shared error list.</summary>
internal sealed class SeedPackage
{
    private static readonly string[] RequiredCategories = ["units", "concepts", "exercises", "blueprints", "lexicon"];

    public List<SeedUnit> Units { get; } = [];

    public List<SeedConcept> Concepts { get; } = [];

    public List<SeedExercise> Exercises { get; } = [];

    public List<SeedBlueprint> Blueprints { get; } = [];

    public List<SeedLexEntry> Lexicon { get; } = [];

    public static SeedPackage? Load(string packageDir, List<string> errors)
    {
        var manifestPath = Path.Combine(packageDir, "manifest.json");
        var manifest = ParseFile(manifestPath, "manifest.json", errors);
        if (manifest is null)
        {
            return null;
        }

        using (manifest)
        {
            var root = manifest.RootElement;
            if (Str(root, "name", "manifest.json", errors).Length == 0)
            {
                // Error already recorded.
            }

            var language = Str(root, "language", "manifest.json", errors);
            if (language.Length > 0 && (language.Length != 2 || !language.All(char.IsAsciiLetterLower)))
            {
                errors.Add($"manifest.json :: language must be a two-letter lowercase code (got '{language}')");
            }

            var version = Str(root, "packageVersion", "manifest.json", errors);
            if (version.Length > 0 && !SeedValidator.ValidSemver(version))
            {
                errors.Add($"manifest.json :: packageVersion must be MAJOR.MINOR.PATCH (got '{version}')");
            }

            if (!root.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Object)
            {
                errors.Add("manifest.json :: missing 'files' object");
                return null;
            }

            var package = new SeedPackage();
            foreach (var category in files.EnumerateObject())
            {
                if (!RequiredCategories.Contains(category.Name))
                {
                    errors.Add($"manifest.json :: unknown files category '{category.Name}'");
                    continue;
                }

                if (category.Value.ValueKind != JsonValueKind.Array)
                {
                    errors.Add($"manifest.json :: files.{category.Name} must be an array of paths");
                    continue;
                }

                foreach (var pathElement in category.Value.EnumerateArray())
                {
                    var relativePath = pathElement.GetString() ?? string.Empty;
                    var fullPath = Path.Combine(packageDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(fullPath))
                    {
                        errors.Add($"manifest.json :: files.{category.Name} references missing file '{relativePath}'");
                        continue;
                    }

                    var document = ParseFile(fullPath, relativePath, errors);
                    if (document is null)
                    {
                        continue;
                    }

                    using (document)
                    {
                        switch (category.Name)
                        {
                            case "units":
                                package.ParseUnits(document.RootElement, relativePath, errors);
                                break;
                            case "concepts":
                                package.ParseConcepts(document.RootElement, relativePath, errors);
                                break;
                            case "exercises":
                                package.ParseExercises(document.RootElement, relativePath, errors);
                                break;
                            case "blueprints":
                                package.ParseBlueprints(document.RootElement, relativePath, errors);
                                break;
                            case "lexicon":
                                package.ParseLexicon(document.RootElement, relativePath, errors);
                                break;
                        }
                    }
                }
            }

            foreach (var category in RequiredCategories.Where(c => !files.TryGetProperty(c, out _)))
            {
                errors.Add($"manifest.json :: missing files category '{category}'");
            }

            return package;
        }
    }

    private void ParseUnits(JsonElement root, string file, List<string> errors)
    {
        foreach (var element in Items(root, "units", file, errors))
        {
            var code = Str(element, "code", file, errors);
            var lessons = new List<SeedLesson>();
            if (element.TryGetProperty("lessonTemplates", out var templates) && templates.ValueKind == JsonValueKind.Array)
            {
                foreach (var template in templates.EnumerateArray())
                {
                    lessons.Add(new SeedLesson(
                        Str(template, "title", file, errors),
                        StrList(template, "concepts", file, errors)));
                }
            }
            else
            {
                errors.Add($"{file} :: unit '{code}': missing 'lessonTemplates' array");
            }

            Str(element, "title", file, errors);
            Str(element, "description", file, errors);
            Units.Add(new SeedUnit(
                file,
                code,
                Str(element, "level", file, errors),
                Int(element, "orderIndex", file, errors),
                lessons));
        }
    }

    private void ParseConcepts(JsonElement root, string file, List<string> errors)
    {
        foreach (var element in Items(root, "concepts", file, errors))
        {
            var code = Str(element, "code", file, errors);
            var context = $"{file} :: concept '{code}'";

            var dependencies = new List<SeedDependency>();
            if (element.TryGetProperty("dependencies", out var deps) && deps.ValueKind == JsonValueKind.Array)
            {
                foreach (var dep in deps.EnumerateArray())
                {
                    dependencies.Add(new SeedDependency(
                        Str(dep, "prerequisite", file, errors),
                        Str(dep, "type", file, errors),
                        OptDbl(dep, "minMastery")));
                }
            }
            else
            {
                errors.Add($"{context}: missing 'dependencies' array (use [] for roots)");
            }

            var mistakeCodes = new List<string>();
            if (element.TryGetProperty("commonMistakes", out var mistakes) && mistakes.ValueKind == JsonValueKind.Array)
            {
                foreach (var mistake in mistakes.EnumerateArray())
                {
                    Str(mistake, "wrong", file, errors);
                    Str(mistake, "right", file, errors);
                    Str(mistake, "explanation", file, errors);
                    mistakeCodes.Add(Str(mistake, "errorCode", file, errors));
                }
            }

            if (element.TryGetProperty("examples", out var examples) && examples.ValueKind == JsonValueKind.Array)
            {
                if (examples.GetArrayLength() == 0)
                {
                    errors.Add($"{context}: at least one example is required");
                }

                foreach (var example in examples.EnumerateArray())
                {
                    Str(example, "de", file, errors);
                    Str(example, "en", file, errors);
                }
            }
            else
            {
                errors.Add($"{context}: missing 'examples' array");
            }

            Str(element, "title", file, errors);
            Str(element, "learningObjective", file, errors);
            Str(element, "explanationMarkdown", file, errors);
            Concepts.Add(new SeedConcept(
                file,
                code,
                Str(element, "kind", file, errors),
                Str(element, "level", file, errors),
                Int(element, "difficulty", file, errors),
                Dbl(element, "masteryThreshold", file, errors),
                Dbl(element, "importance", file, errors),
                Int(element, "estimatedMinutes", file, errors),
                Str(element, "unit", file, errors),
                dependencies,
                mistakeCodes,
                StrList(element, "requiredVocabulary", file, errors)));
        }
    }

    private void ParseExercises(JsonElement root, string file, List<string> errors)
    {
        foreach (var element in Items(root, "exercises", file, errors))
        {
            Exercises.Add(new SeedExercise(
                file,
                Str(element, "code", file, errors),
                Str(element, "type", file, errors),
                StrList(element, "concepts", file, errors),
                Int(element, "difficulty", file, errors),
                Str(element, "stem", file, errors),
                StrList(element, "options", file, errors),
                Str(element, "answerKey", file, errors),
                OptStr(element, "errorCode")));
        }
    }

    private void ParseBlueprints(JsonElement root, string file, List<string> errors)
    {
        foreach (var element in Items(root, "blueprints", file, errors))
        {
            var sections = new List<SeedBlueprintSection>();
            if (element.TryGetProperty("sections", out var sectionArray) && sectionArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var section in sectionArray.EnumerateArray())
                {
                    sections.Add(new SeedBlueprintSection(
                        Str(section, "skill", file, errors),
                        Int(section, "itemCount", file, errors),
                        Dbl(section, "passPercentage", file, errors)));
                }
            }

            Blueprints.Add(new SeedBlueprint(
                file,
                Str(element, "kind", file, errors),
                Str(element, "level", file, errors),
                OptStr(element, "unit"),
                sections));
        }
    }

    private void ParseLexicon(JsonElement root, string file, List<string> errors)
    {
        if (root.TryGetProperty("defaults", out var defaults) &&
            defaults.TryGetProperty("level", out var defaultLevel) &&
            !SeedValidator.ValidCefr(defaultLevel.GetString() ?? string.Empty))
        {
            errors.Add($"{file} :: defaults.level is not a valid CEFR sub-level");
        }

        foreach (var element in Items(root, "entries", file, errors))
        {
            var lemma = Str(element, "lemma", file, errors);
            if (element.TryGetProperty("senses", out var senses) && senses.ValueKind == JsonValueKind.Array)
            {
                foreach (var sense in senses.EnumerateArray())
                {
                    Str(sense, "meaning", file, errors);
                    Str(sense, "exampleDe", file, errors);
                    Str(sense, "exampleEn", file, errors);
                    Str(sense, "topic", file, errors);
                }
            }

            Lexicon.Add(new SeedLexEntry(
                file,
                lemma,
                Str(element, "pos", file, errors),
                OptStr(element, "gender"),
                OptBool(element, "separable"),
                Int(element, "difficulty", file, errors),
                OptInt(element, "frequencyRank"),
                element.TryGetProperty("senses", out var s) && s.ValueKind == JsonValueKind.Array ? s.GetArrayLength() : 0));
        }
    }

    private static JsonDocument? ParseFile(string fullPath, string displayName, List<string> errors)
    {
        try
        {
            return JsonDocument.Parse(File.ReadAllText(fullPath));
        }
        catch (FileNotFoundException)
        {
            errors.Add($"{displayName} :: file not found");
            return null;
        }
        catch (JsonException exception)
        {
            errors.Add($"{displayName} :: invalid JSON — {exception.Message}");
            return null;
        }
    }

    private static List<JsonElement> Items(JsonElement root, string property, string file, List<string> errors)
    {
        if (!root.TryGetProperty(property, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            errors.Add($"{file} :: missing root array '{property}'");
            return [];
        }

        return [.. array.EnumerateArray()];
    }

    private static string Str(JsonElement element, string property, string file, List<string> errors)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString()!;
            if (text.Length > 0)
            {
                return text;
            }
        }

        errors.Add($"{file} :: missing or empty string '{property}' in {Describe(element)}");
        return string.Empty;
    }

    private static string? OptStr(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int Int(JsonElement element, string property, string file, List<string> errors)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        errors.Add($"{file} :: missing or non-integer '{property}' in {Describe(element)}");
        return 0;
    }

    private static int? OptInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            ? number
            : null;

    private static double Dbl(JsonElement element, string property, string file, List<string> errors)
    {
        if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number)
        {
            return value.GetDouble();
        }

        errors.Add($"{file} :: missing or non-numeric '{property}' in {Describe(element)}");
        return 0;
    }

    private static double? OptDbl(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;

    private static bool? OptBool(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    private static List<string> StrList(JsonElement element, string property, string file, List<string> errors)
    {
        var result = new List<string>();
        if (!element.TryGetProperty(property, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            errors.Add($"{file} :: missing array '{property}' in {Describe(element)}");
            return result;
        }

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { Length: > 0 } text)
            {
                result.Add(text);
            }
            else
            {
                errors.Add($"{file} :: '{property}' contains a non-string or empty item in {Describe(element)}");
            }
        }

        return result;
    }

    private static string Describe(JsonElement element) =>
        element.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String
            ? $"'{code.GetString()}'"
            : element.TryGetProperty("lemma", out var lemma) && lemma.ValueKind == JsonValueKind.String
                ? $"'{lemma.GetString()}'"
                : "an object without code/lemma";
}
