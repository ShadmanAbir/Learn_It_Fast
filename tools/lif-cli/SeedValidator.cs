using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lif.Cli;

/// <summary>
/// Validates a curriculum seed package (doc 18 §4): manifest completeness, schema shape,
/// dependency DAG acyclicity, CEFR consistency, cross-file references (units ↔ concepts ↔
/// exercises ↔ lexicon) and error-code taxonomy usage. Exit code 0 = valid, 1 = violations.
/// </summary>
internal static partial class SeedValidator
{
    private static readonly string[] ValidPos =
        ["noun", "verb", "adj", "adv", "prep", "pron", "conj", "num", "particle", "phrase"];

    private static readonly string[] ValidGenders = ["der", "die", "das"];

    private static readonly string[] ValidConceptKinds = ["grammar", "function", "culture"];

    private static readonly string[] ValidExerciseTypes = ["multiple_choice", "cloze", "matching", "ordering"];

    private static readonly string[] ValidBlueprintKinds = ["checkpoint", "level_up", "placement_bank"];

    private static readonly string[] ValidSkills =
        ["reading", "writing", "listening", "speaking", "grammar", "vocabulary", "pronunciation"];

    [GeneratedRegex(@"^(A1|A2|B1|B2|C1)\.[12]$")]
    private static partial Regex CefrPattern();

    [GeneratedRegex(@"^[a-z][a-z0-9]*(\.[a-z0-9-]+)+$")]
    private static partial Regex ConceptCodePattern();

    [GeneratedRegex(@"^ex(\.[a-z0-9-]+)+$")]
    private static partial Regex ExerciseCodePattern();

    [GeneratedRegex(@"^err(\.[a-z0-9-]+)+$")]
    private static partial Regex ErrorCodePattern();

    [GeneratedRegex(@"^\d+\.\d+\.\d+$")]
    private static partial Regex SemverPattern();

    public static int Run(string packageDir)
    {
        if (!Directory.Exists(packageDir))
        {
            Console.Error.WriteLine($"error: package directory not found: {packageDir}");
            return 1;
        }

        var errors = new List<string>();
        var package = SeedPackage.Load(packageDir, errors);
        if (package is not null)
        {
            ValidateLexicon(package, errors);
            ValidateConcepts(package, errors);
            ValidateUnits(package, errors);
            ValidateExercises(package, errors);
            ValidateBlueprints(package, errors);
        }

        if (errors.Count > 0)
        {
            Console.Error.WriteLine($"seed-validate: {errors.Count} error(s) in {packageDir}");
            foreach (var error in errors)
            {
                Console.Error.WriteLine($"  - {error}");
            }

            return 1;
        }

        Console.WriteLine(
            $"seed-validate: OK — {package!.Concepts.Count} concepts, {package.Units.Count} units, " +
            $"{package.Exercises.Count} exercises, {package.Blueprints.Count} blueprints, " +
            $"{package.Lexicon.Count} lexicon entries ({packageDir})");
        return 0;
    }

    private static void ValidateLexicon(SeedPackage package, List<string> errors)
    {
        var seen = new HashSet<(string Lemma, string Pos)>();
        foreach (var entry in package.Lexicon)
        {
            var where = $"{entry.File} :: lexicon '{entry.Lemma}'";
            if (string.IsNullOrWhiteSpace(entry.Lemma))
            {
                errors.Add($"{entry.File} :: lexicon entry with empty lemma");
                continue;
            }

            if (!seen.Add((entry.Lemma, entry.Pos)))
            {
                errors.Add($"{where}: duplicate (lemma, pos) pair");
            }

            if (!ValidPos.Contains(entry.Pos))
            {
                errors.Add($"{where}: invalid pos '{entry.Pos}'");
            }

            if (entry.Pos == "noun")
            {
                // 'Eltern' is plural-only and still needs its article; gender is mandatory for nouns.
                if (entry.Gender is null || !ValidGenders.Contains(entry.Gender))
                {
                    errors.Add($"{where}: noun requires gender der|die|das (got '{entry.Gender}')");
                }
            }
            else if (entry.Gender is not null)
            {
                errors.Add($"{where}: gender is only valid for nouns");
            }

            if (entry.Separable is not null && entry.Pos != "verb")
            {
                errors.Add($"{where}: 'separable' is only valid for verbs");
            }

            if (entry.Difficulty is < 1 or > 10)
            {
                errors.Add($"{where}: difficulty must be 1..10 (got {entry.Difficulty})");
            }

            if (entry.FrequencyRank is < 1)
            {
                errors.Add($"{where}: frequencyRank must be >= 1");
            }

            if (entry.SenseCount == 0)
            {
                errors.Add($"{where}: at least one sense is required");
            }
        }
    }

    private static void ValidateConcepts(SeedPackage package, List<string> errors)
    {
        var unitsByCode = FirstByKey(package.Units, u => u.Code);
        var codes = new HashSet<string>();

        foreach (var concept in package.Concepts)
        {
            var where = $"{concept.File} :: concept '{concept.Code}'";
            if (!codes.Add(concept.Code))
            {
                errors.Add($"{where}: duplicate concept code");
            }

            if (!ConceptCodePattern().IsMatch(concept.Code))
            {
                errors.Add($"{where}: code must match '{ConceptCodePattern()}'");
            }

            if (!ValidConceptKinds.Contains(concept.Kind))
            {
                errors.Add($"{where}: invalid kind '{concept.Kind}'");
            }

            if (!CefrPattern().IsMatch(concept.Level))
            {
                errors.Add($"{where}: invalid CEFR level '{concept.Level}'");
            }

            if (concept.Difficulty is < 1 or > 10)
            {
                errors.Add($"{where}: difficulty must be 1..10 (got {concept.Difficulty})");
            }

            if (concept.MasteryThreshold is <= 0 or > 1)
            {
                errors.Add($"{where}: masteryThreshold must be in (0, 1]");
            }

            if (concept.Importance is < 0 or > 1)
            {
                errors.Add($"{where}: importance must be in [0, 1]");
            }

            if (concept.EstimatedMinutes < 1)
            {
                errors.Add($"{where}: estimatedMinutes must be >= 1");
            }

            if (!unitsByCode.TryGetValue(concept.Unit, out var unit))
            {
                errors.Add($"{where}: unknown unit '{concept.Unit}'");
            }
            else if (unit.Level != concept.Level)
            {
                errors.Add($"{where}: level {concept.Level} does not match unit {unit.Code} level {unit.Level}");
            }

            foreach (var mistakeCode in concept.MistakeErrorCodes)
            {
                if (!ErrorCodePattern().IsMatch(mistakeCode))
                {
                    errors.Add($"{where}: invalid errorCode '{mistakeCode}'");
                }
            }

            ValidateDependencies(concept, package, errors);
            ValidateRequiredVocabulary(concept, package, errors);
        }

        DetectDependencyCycles(package, errors);
    }

    private static void ValidateDependencies(SeedConcept concept, SeedPackage package, List<string> errors)
    {
        var where = $"{concept.File} :: concept '{concept.Code}'";
        var conceptCodes = package.Concepts.Select(c => c.Code).ToHashSet();
        var seen = new HashSet<string>();

        foreach (var dependency in concept.Dependencies)
        {
            if (!conceptCodes.Contains(dependency.Prerequisite))
            {
                errors.Add($"{where}: unknown prerequisite '{dependency.Prerequisite}'");
            }

            if (dependency.Prerequisite == concept.Code)
            {
                errors.Add($"{where}: concept depends on itself");
            }

            if (!seen.Add(dependency.Prerequisite))
            {
                errors.Add($"{where}: duplicate dependency on '{dependency.Prerequisite}'");
            }

            if (dependency.Type is not ("requires" or "supports"))
            {
                errors.Add($"{where}: dependency type must be requires|supports (got '{dependency.Type}')");
            }

            if (dependency.Type == "requires" && dependency.MinMastery is null or <= 0 or > 1)
            {
                errors.Add($"{where}: 'requires' dependency on '{dependency.Prerequisite}' needs minMastery in (0, 1]");
            }

            if (dependency.Type == "supports" && dependency.MinMastery is not null)
            {
                errors.Add($"{where}: 'supports' dependency on '{dependency.Prerequisite}' must not set minMastery");
            }
        }
    }

    private static void ValidateRequiredVocabulary(SeedConcept concept, SeedPackage package, List<string> errors)
    {
        var where = $"{concept.File} :: concept '{concept.Code}'";
        foreach (var reference in concept.RequiredVocabulary)
        {
            // Noun references carry their article ("der Tisch"); the lexicon stores the bare
            // lemma plus a gender field. Anything without an article resolves case-sensitively.
            var parts = reference.Split(' ', 2);
            if (parts.Length == 2 && ValidGenders.Contains(parts[0]))
            {
                var noun = package.Lexicon.FirstOrDefault(e => e.Lemma == parts[1] && e.Pos == "noun");
                if (noun is null)
                {
                    errors.Add($"{where}: requiredVocabulary '{reference}' has no noun lemma '{parts[1]}' in the lexicon");
                }
                else if (noun.Gender != parts[0])
                {
                    errors.Add($"{where}: requiredVocabulary '{reference}' conflicts with lexicon gender '{noun.Gender}'");
                }
            }
            else if (!package.Lexicon.Any(e => e.Lemma == reference))
            {
                errors.Add($"{where}: requiredVocabulary '{reference}' not found in the lexicon");
            }
        }
    }

    private static void DetectDependencyCycles(SeedPackage package, List<string> errors)
    {
        var edges = FirstByKey(package.Concepts, c => c.Code)
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Dependencies.Select(d => d.Prerequisite).Where(p => p != pair.Key).ToList());

        // 0 = unvisited, 1 = on stack, 2 = done.
        var state = edges.Keys.ToDictionary(code => code, _ => 0);
        var stack = new Stack<string>();

        foreach (var start in edges.Keys.OrderBy(c => c, StringComparer.Ordinal))
        {
            if (state[start] == 0 && Visit(start))
            {
                return; // Report at most one cycle; fixing it may dissolve the rest.
            }
        }

        return;

        bool Visit(string code)
        {
            state[code] = 1;
            stack.Push(code);
            foreach (var prerequisite in edges[code].Where(edges.ContainsKey))
            {
                if (state[prerequisite] == 1)
                {
                    var cycle = stack.TakeWhile(c => c != prerequisite).Append(prerequisite).Reverse();
                    errors.Add($"dependency cycle: {string.Join(" -> ", cycle)} -> {prerequisite}");
                    return true;
                }

                if (state[prerequisite] == 0 && Visit(prerequisite))
                {
                    return true;
                }
            }

            stack.Pop();
            state[code] = 2;
            return false;
        }
    }

    private static void ValidateUnits(SeedPackage package, List<string> errors)
    {
        var conceptsByCode = FirstByKey(package.Concepts, c => c.Code);
        var codes = new HashSet<string>();
        var orderIndexes = new HashSet<int>();
        var lessonCoverage = new HashSet<string>();

        foreach (var unit in package.Units)
        {
            var where = $"{unit.File} :: unit '{unit.Code}'";
            if (!codes.Add(unit.Code))
            {
                errors.Add($"{where}: duplicate unit code");
            }

            if (!CefrPattern().IsMatch(unit.Level))
            {
                errors.Add($"{where}: invalid CEFR level '{unit.Level}'");
            }

            if (!orderIndexes.Add(unit.OrderIndex))
            {
                errors.Add($"{where}: duplicate orderIndex {unit.OrderIndex}");
            }

            if (unit.Lessons.Count == 0)
            {
                errors.Add($"{where}: at least one lesson template is required");
            }

            foreach (var lesson in unit.Lessons)
            {
                if (lesson.Concepts.Count == 0)
                {
                    errors.Add($"{where}: lesson '{lesson.Title}' lists no concepts");
                }

                foreach (var code in lesson.Concepts)
                {
                    lessonCoverage.Add(code);
                    if (!conceptsByCode.TryGetValue(code, out var concept))
                    {
                        errors.Add($"{where}: lesson '{lesson.Title}' references unknown concept '{code}'");
                    }
                    else if (concept.Unit != unit.Code)
                    {
                        errors.Add($"{where}: lesson '{lesson.Title}' pulls '{code}' from unit '{concept.Unit}'");
                    }
                }
            }
        }

        foreach (var concept in package.Concepts.Where(c => !lessonCoverage.Contains(c.Code)))
        {
            errors.Add($"{concept.File} :: concept '{concept.Code}': not covered by any lesson template");
        }
    }

    private static void ValidateExercises(SeedPackage package, List<string> errors)
    {
        var conceptsByCode = FirstByKey(package.Concepts, c => c.Code);
        var codes = new HashSet<string>();
        var exercisedConcepts = new HashSet<string>();

        foreach (var exercise in package.Exercises)
        {
            var where = $"{exercise.File} :: exercise '{exercise.Code}'";
            if (!codes.Add(exercise.Code))
            {
                errors.Add($"{where}: duplicate exercise code");
            }

            if (!ExerciseCodePattern().IsMatch(exercise.Code))
            {
                errors.Add($"{where}: code must match '{ExerciseCodePattern()}'");
            }

            if (!ValidExerciseTypes.Contains(exercise.Type))
            {
                errors.Add($"{where}: invalid type '{exercise.Type}'");
            }

            if (exercise.Difficulty is < 1 or > 10)
            {
                errors.Add($"{where}: difficulty must be 1..10 (got {exercise.Difficulty})");
            }

            if (string.IsNullOrWhiteSpace(exercise.Stem))
            {
                errors.Add($"{where}: stem is required");
            }

            if (string.IsNullOrWhiteSpace(exercise.AnswerKey))
            {
                errors.Add($"{where}: answerKey is required");
            }

            if (exercise.Concepts.Count == 0)
            {
                errors.Add($"{where}: at least one concept reference is required");
            }

            var referencedConcepts = new List<SeedConcept>();
            foreach (var code in exercise.Concepts)
            {
                exercisedConcepts.Add(code);
                if (conceptsByCode.TryGetValue(code, out var concept))
                {
                    referencedConcepts.Add(concept);
                }
                else
                {
                    errors.Add($"{where}: unknown concept '{code}'");
                }
            }

            if (exercise.ErrorCode is not null &&
                !referencedConcepts.Any(c => c.MistakeErrorCodes.Contains(exercise.ErrorCode)))
            {
                errors.Add($"{where}: errorCode '{exercise.ErrorCode}' is not declared by any referenced concept's commonMistakes");
            }

            ValidateAnswerKey(exercise, errors);
        }

        foreach (var concept in package.Concepts.Where(c => !exercisedConcepts.Contains(c.Code)))
        {
            errors.Add($"{concept.File} :: concept '{concept.Code}': has no curated exercise");
        }
    }

    private static void ValidateAnswerKey(SeedExercise exercise, List<string> errors)
    {
        var where = $"{exercise.File} :: exercise '{exercise.Code}'";
        switch (exercise.Type)
        {
            case "multiple_choice":
                if (exercise.Options.Count < 2)
                {
                    errors.Add($"{where}: multiple_choice needs at least 2 options");
                }

                if (exercise.Options.Distinct(StringComparer.Ordinal).Count() != exercise.Options.Count)
                {
                    errors.Add($"{where}: options contain duplicates");
                }

                if (!exercise.Options.Contains(exercise.AnswerKey, StringComparer.Ordinal))
                {
                    errors.Add($"{where}: answerKey is not one of the options");
                }

                break;

            case "cloze":
                if (!exercise.Stem.Contains("___", StringComparison.Ordinal))
                {
                    errors.Add($"{where}: cloze stem must contain a '___' gap");
                }

                if (exercise.Options.Count != 0)
                {
                    errors.Add($"{where}: cloze must not declare options");
                }

                break;

            case "matching":
                if (exercise.Options.Count != 0)
                {
                    errors.Add($"{where}: matching must not declare options (presentation derives from the key)");
                }

                var pairs = exercise.AnswerKey.Split('|');
                if (pairs.Length < 2)
                {
                    errors.Add($"{where}: matching needs at least 2 pairs");
                }

                var lefts = new HashSet<string>(StringComparer.Ordinal);
                foreach (var pair in pairs)
                {
                    var sides = pair.Split('=');
                    if (sides.Length != 2 || string.IsNullOrWhiteSpace(sides[0]) || string.IsNullOrWhiteSpace(sides[1]))
                    {
                        errors.Add($"{where}: malformed pair '{pair}' (expected left=right)");
                    }
                    else if (!lefts.Add(sides[0]))
                    {
                        errors.Add($"{where}: duplicate left side '{sides[0]}'");
                    }
                }

                break;

            case "ordering":
                var tokens = exercise.AnswerKey.Split('|');
                if (tokens.Length < 2)
                {
                    errors.Add($"{where}: ordering needs at least 2 tokens");
                }

                if (!tokens.Order(StringComparer.Ordinal).SequenceEqual(
                        exercise.Options.Order(StringComparer.Ordinal), StringComparer.Ordinal))
                {
                    errors.Add($"{where}: options must be exactly the answerKey tokens (shuffled)");
                }

                if (tokens.SequenceEqual(exercise.Options, StringComparer.Ordinal))
                {
                    errors.Add($"{where}: options must be shuffled, not in the solution order");
                }

                break;
        }
    }

    private static void ValidateBlueprints(SeedPackage package, List<string> errors)
    {
        var unitsByCode = FirstByKey(package.Units, u => u.Code);
        foreach (var blueprint in package.Blueprints)
        {
            var where = $"{blueprint.File} :: blueprint '{blueprint.Kind} {blueprint.Level}'";
            if (!ValidBlueprintKinds.Contains(blueprint.Kind))
            {
                errors.Add($"{where}: invalid kind '{blueprint.Kind}'");
            }

            if (!CefrPattern().IsMatch(blueprint.Level))
            {
                errors.Add($"{where}: invalid CEFR level '{blueprint.Level}'");
            }

            if (blueprint.Kind == "checkpoint" && blueprint.Unit is null)
            {
                errors.Add($"{where}: checkpoint blueprints must reference a unit");
            }

            if (blueprint.Unit is not null)
            {
                if (!unitsByCode.TryGetValue(blueprint.Unit, out var unit))
                {
                    errors.Add($"{where}: unknown unit '{blueprint.Unit}'");
                }
                else if (unit.Level != blueprint.Level)
                {
                    errors.Add($"{where}: level {blueprint.Level} does not match unit level {unit.Level}");
                }
            }

            if (blueprint.Sections.Count == 0)
            {
                errors.Add($"{where}: at least one section is required");
            }

            foreach (var section in blueprint.Sections)
            {
                if (!ValidSkills.Contains(section.Skill))
                {
                    errors.Add($"{where}: invalid skill '{section.Skill}'");
                }

                if (section.ItemCount < 1)
                {
                    errors.Add($"{where}: itemCount must be >= 1");
                }

                if (section.PassPercentage is <= 0 or > 100)
                {
                    errors.Add($"{where}: passPercentage must be in (0, 100]");
                }
            }
        }
    }

    internal static bool ValidSemver(string version) => SemverPattern().IsMatch(version);

    internal static bool ValidCefr(string level) => CefrPattern().IsMatch(level);

    /// <summary>Duplicate-tolerant lookup — duplicates are reported as errors elsewhere.</summary>
    private static Dictionary<string, T> FirstByKey<T>(IEnumerable<T> items, Func<T, string> key)
    {
        var result = new Dictionary<string, T>();
        foreach (var item in items)
        {
            result.TryAdd(key(item), item);
        }

        return result;
    }
}
