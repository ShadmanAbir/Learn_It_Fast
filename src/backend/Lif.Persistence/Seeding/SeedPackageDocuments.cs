using System.Text.Json;

namespace Lif.Persistence.Seeding;

/// <summary>
/// Raw seed package documents (ADR-014 layout, same shape the lif-cli validator checks).
/// Camel-case JSON; documents are trusted here — run <c>lif-cli validate-seeds</c> upstream.
/// </summary>
internal sealed record SeedManifest(string Name, string Language, string PackageVersion, SeedManifestFiles Files);

internal sealed record SeedManifestFiles(
    IReadOnlyList<string> Units,
    IReadOnlyList<string> Concepts,
    IReadOnlyList<string> Exercises,
    IReadOnlyList<string> Blueprints,
    IReadOnlyList<string> Lexicon);

internal sealed record SeedUnitsDoc(IReadOnlyList<SeedUnit> Units);

internal sealed record SeedUnit(
    string Code,
    string Level,
    int OrderIndex,
    string Title,
    string? Description,
    string? CultureNote,
    IReadOnlyList<SeedLessonTemplate> LessonTemplates);

internal sealed record SeedLessonTemplate(string Title, IReadOnlyList<string> Concepts);

internal sealed record SeedConceptsDoc(IReadOnlyList<SeedConcept> Concepts);

internal sealed record SeedConcept(
    string Code,
    string Kind,
    string Level,
    int Difficulty,
    string Title,
    string LearningObjective,
    string ExplanationMarkdown,
    int EstimatedMinutes,
    double MasteryThreshold,
    double Importance,
    string Unit,
    IReadOnlyList<SeedDependency> Dependencies,
    IReadOnlyList<SeedExample> Examples,
    IReadOnlyList<SeedCommonMistake> CommonMistakes,
    IReadOnlyList<string> RequiredVocabulary);

internal sealed record SeedDependency(string Prerequisite, string Type, double? MinMastery);

internal sealed record SeedExample(string De, string En, string? Note);

internal sealed record SeedCommonMistake(string Wrong, string Right, string Explanation, string ErrorCode);

internal sealed record SeedExercisesDoc(IReadOnlyList<SeedExercise> Exercises);

internal sealed record SeedExercise(
    string Code,
    string Type,
    IReadOnlyList<string> Concepts,
    int Difficulty,
    string Stem,
    IReadOnlyList<string> Options,
    string AnswerKey,
    string? Explanation,
    string? ErrorCode);

internal sealed record SeedBlueprintsDoc(IReadOnlyList<SeedBlueprint> Blueprints);

internal sealed record SeedBlueprint(string Kind, string Level, string? Unit, IReadOnlyList<SeedBlueprintSection> Sections);

internal sealed record SeedBlueprintSection(string Skill, int ItemCount, double PassPercentage);

internal sealed record SeedLexiconDoc(SeedLexiconDefaults? Defaults, IReadOnlyList<SeedLexEntry> Entries);

internal sealed record SeedLexiconDefaults(string? Level);

internal sealed record SeedLexEntry(
    string Lemma,
    string Pos,
    string? Gender,
    string? Plural,
    string? Ipa,
    int? FrequencyRank,
    int Difficulty,
    bool? Separable,
    IReadOnlyList<SeedSense> Senses);

internal sealed record SeedSense(string Meaning, string ExampleDe, string ExampleEn, string Topic);

/// <summary>Loads and deserializes the seed package documents referenced by a manifest.</summary>
internal static class SeedPackageReader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static SeedManifest ReadManifest(string packageDirectory) =>
        Read<SeedManifest>(Path.Combine(packageDirectory, "manifest.json"));

    public static IEnumerable<TDoc> ReadAll<TDoc>(string packageDirectory, IReadOnlyList<string> relativePaths) =>
        relativePaths.Select(path => Read<TDoc>(Path.Combine(packageDirectory, path)));

    private static TDoc Read<TDoc>(string path) =>
        JsonSerializer.Deserialize<TDoc>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException($"Seed file '{path}' deserialized to null.");
}
