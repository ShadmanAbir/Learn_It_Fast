using Lif.Domain.Curriculum;
using Lif.Domain.Learning;
using Lif.SharedKernel.Primitives;
using Lif.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Lif.Persistence.Seeding;

/// <summary>Fully resolved seed package: domain aggregates ready to persist as one unit.</summary>
public sealed class CurriculumSeedModel
{
    internal CurriculumSeedModel(
        CurriculumVersion version,
        IReadOnlyList<Unit> units,
        IReadOnlyList<Concept> concepts,
        IReadOnlyList<VocabEntry> vocabEntries,
        IReadOnlyList<CuratedExercise> exercises,
        IReadOnlyList<AssessmentBlueprint> blueprints)
    {
        Version = version;
        Units = units;
        Concepts = concepts;
        VocabEntries = vocabEntries;
        Exercises = exercises;
        Blueprints = blueprints;
    }

    public CurriculumVersion Version { get; }

    public IReadOnlyList<Unit> Units { get; }

    public IReadOnlyList<Concept> Concepts { get; }

    public IReadOnlyList<VocabEntry> VocabEntries { get; }

    public IReadOnlyList<CuratedExercise> Exercises { get; }

    public IReadOnlyList<AssessmentBlueprint> Blueprints { get; }
}

/// <summary>
/// Imports a seed content package (ADR-014) into the database through domain factories.
/// Build order resolves the reference graph: version → lexicon → units → concepts (create,
/// then dependencies + vocab refs) → lesson templates → curated exercises → blueprints.
/// Idempotent on (language, packageVersion) — an already-imported package is left untouched.
/// </summary>
public static class CurriculumSeedImporter
{
    /// <summary>Parses and resolves the package into aggregates without touching the database.</summary>
    public static CurriculumSeedModel Build(string packageDirectory)
    {
        var manifest = SeedPackageReader.ReadManifest(packageDirectory);
        var version = CurriculumVersion.CreateDraft(LanguageCode.From(manifest.Language), manifest.PackageVersion);

        var vocabEntries = BuildLexicon(packageDirectory, manifest, version.Id, out var vocabByRef);
        var units = BuildUnits(packageDirectory, manifest, version.Id, out var unitsByCode);
        var concepts = BuildConcepts(packageDirectory, manifest, version.Id, unitsByCode, vocabByRef, out var conceptsByCode);
        AddLessonTemplates(packageDirectory, manifest, unitsByCode, conceptsByCode);
        var exercises = BuildExercises(packageDirectory, manifest, version.Id, conceptsByCode);
        var blueprints = BuildBlueprints(packageDirectory, manifest, version.Id, unitsByCode);

        // Seed packages ship finished content; learners onboard against a published version.
        version.Publish();

        return new CurriculumSeedModel(version, units, concepts, vocabEntries, exercises, blueprints);
    }

    /// <summary>Imports the package unless (language, packageVersion) is already present; returns the version id.</summary>
    public static async Task<Guid> ImportAsync(LifDbContext context, string packageDirectory, CancellationToken cancellationToken = default)
    {
        var manifest = SeedPackageReader.ReadManifest(packageDirectory);
        var language = LanguageCode.From(manifest.Language);

        var existing = await context.CurriculumVersions
            .FirstOrDefaultAsync(v => v.Language == language && v.Version == manifest.PackageVersion, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var model = Build(packageDirectory);
        context.CurriculumVersions.Add(model.Version);
        context.VocabEntries.AddRange(model.VocabEntries);
        context.Units.AddRange(model.Units);
        context.Concepts.AddRange(model.Concepts);
        context.CuratedExercises.AddRange(model.Exercises);
        context.AssessmentBlueprints.AddRange(model.Blueprints);
        await context.SaveChangesAsync(cancellationToken);

        return model.Version.Id;
    }

    private static List<VocabEntry> BuildLexicon(
        string packageDirectory, SeedManifest manifest, Guid versionId, out Dictionary<string, VocabEntry> vocabByRef)
    {
        var entries = new List<VocabEntry>();
        vocabByRef = new Dictionary<string, VocabEntry>(StringComparer.Ordinal);

        foreach (var doc in SeedPackageReader.ReadAll<SeedLexiconDoc>(packageDirectory, manifest.Files.Lexicon))
        {
            var defaultLevel = Cefr(doc.Defaults?.Level ?? "A1.1");
            foreach (var seed in doc.Entries)
            {
                var entry = Require(
                    VocabEntry.Create(
                        versionId, seed.Lemma, seed.Pos, defaultLevel, Difficulty.From(seed.Difficulty),
                        seed.Gender, seed.Plural, seed.Ipa, seed.FrequencyRank, seed.Separable),
                    $"vocab '{seed.Lemma}'");
                foreach (var sense in seed.Senses)
                {
                    entry.AddSense(sense.Meaning, sense.ExampleDe, sense.ExampleEn, sense.Topic);
                }

                // Concepts reference nouns with their article ("der Morgen"), everything else bare.
                // Ordinal keys keep case-sensitive pairs like pron "sie" / "Sie" apart.
                var reference = seed.Gender is null ? seed.Lemma : $"{seed.Gender} {seed.Lemma}";
                if (!vocabByRef.TryAdd(reference, entry))
                {
                    throw new InvalidDataException($"Duplicate lexicon reference '{reference}'.");
                }

                entries.Add(entry);
            }
        }

        return entries;
    }

    private static List<Unit> BuildUnits(
        string packageDirectory, SeedManifest manifest, Guid versionId, out Dictionary<string, Unit> unitsByCode)
    {
        var units = new List<Unit>();
        unitsByCode = new Dictionary<string, Unit>(StringComparer.Ordinal);

        foreach (var doc in SeedPackageReader.ReadAll<SeedUnitsDoc>(packageDirectory, manifest.Files.Units))
        {
            foreach (var seed in doc.Units)
            {
                var unit = Unit.Create(versionId, Cefr(seed.Level), seed.OrderIndex, seed.Title, seed.Description, seed.CultureNote);
                if (!unitsByCode.TryAdd(seed.Code, unit))
                {
                    throw new InvalidDataException($"Duplicate unit code '{seed.Code}'.");
                }

                units.Add(unit);
            }
        }

        return units;
    }

    private static List<Concept> BuildConcepts(
        string packageDirectory,
        SeedManifest manifest,
        Guid versionId,
        Dictionary<string, Unit> unitsByCode,
        Dictionary<string, VocabEntry> vocabByRef,
        out Dictionary<string, Concept> conceptsByCode)
    {
        var docs = SeedPackageReader.ReadAll<SeedConceptsDoc>(packageDirectory, manifest.Files.Concepts).ToList();
        var concepts = new List<Concept>();
        conceptsByCode = new Dictionary<string, Concept>(StringComparer.Ordinal);

        // Pass 1 — create every concept so pass 2 can resolve dependency edges in any order.
        foreach (var seed in docs.SelectMany(d => d.Concepts))
        {
            var concept = Require(
                Concept.Create(
                    versionId, seed.Code, EnumText.Read<ConceptKind>(seed.Kind), Cefr(seed.Level),
                    Difficulty.From(seed.Difficulty), seed.Title, seed.LearningObjective, seed.ExplanationMarkdown,
                    seed.EstimatedMinutes, seed.MasteryThreshold, seed.Importance,
                    Resolve(unitsByCode, seed.Unit, "unit", seed.Code).Id),
                $"concept '{seed.Code}'");

            foreach (var example in seed.Examples)
            {
                concept.AddExample(example.De, example.En, example.Note);
            }

            foreach (var mistake in seed.CommonMistakes)
            {
                concept.AddCommonMistake(mistake.Wrong, mistake.Right, mistake.Explanation, mistake.ErrorCode);
            }

            if (!conceptsByCode.TryAdd(seed.Code, concept))
            {
                throw new InvalidDataException($"Duplicate concept code '{seed.Code}'.");
            }

            concepts.Add(concept);
        }

        // Pass 2 — dependency edges and vocabulary references.
        foreach (var seed in docs.SelectMany(d => d.Concepts))
        {
            var concept = conceptsByCode[seed.Code];
            foreach (var dependency in seed.Dependencies)
            {
                var prerequisite = Resolve(conceptsByCode, dependency.Prerequisite, "prerequisite", seed.Code);
                Require(
                    concept.AddDependency(prerequisite.Id, EnumText.Read<DependencyType>(dependency.Type), dependency.MinMastery ?? 0.80),
                    $"concept '{seed.Code}' → '{dependency.Prerequisite}'");
            }

            foreach (var reference in seed.RequiredVocabulary)
            {
                concept.AddRequiredVocabulary(Resolve(vocabByRef, reference, "vocabulary", seed.Code).Id);
            }
        }

        return concepts;
    }

    private static void AddLessonTemplates(
        string packageDirectory, SeedManifest manifest, Dictionary<string, Unit> unitsByCode, Dictionary<string, Concept> conceptsByCode)
    {
        foreach (var doc in SeedPackageReader.ReadAll<SeedUnitsDoc>(packageDirectory, manifest.Files.Units))
        {
            foreach (var seed in doc.Units)
            {
                var unit = unitsByCode[seed.Code];
                foreach (var lesson in seed.LessonTemplates)
                {
                    var conceptIds = lesson.Concepts
                        .Select(code => Resolve(conceptsByCode, code, "concept", seed.Code).Id)
                        .ToList();
                    unit.AddLessonTemplate(lesson.Title, conceptIds);
                }
            }
        }
    }

    private static List<CuratedExercise> BuildExercises(
        string packageDirectory, SeedManifest manifest, Guid versionId, Dictionary<string, Concept> conceptsByCode)
    {
        var exercises = new List<CuratedExercise>();
        var seenCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var doc in SeedPackageReader.ReadAll<SeedExercisesDoc>(packageDirectory, manifest.Files.Exercises))
        {
            foreach (var seed in doc.Exercises)
            {
                if (!seenCodes.Add(seed.Code))
                {
                    throw new InvalidDataException($"Duplicate exercise code '{seed.Code}'.");
                }

                var spec = new ExerciseSpec(
                    EnumText.Read<ExerciseType>(seed.Type),
                    seed.Stem,
                    seed.Options,
                    seed.AnswerKey,
                    seed.Concepts.Select(code => Resolve(conceptsByCode, code, "concept", seed.Code).Id).ToList(),
                    Difficulty.From(seed.Difficulty),
                    seed.Explanation,
                    seed.ErrorCode);
                exercises.Add(Require(CuratedExercise.Create(versionId, seed.Code, spec), $"exercise '{seed.Code}'"));
            }
        }

        return exercises;
    }

    private static List<AssessmentBlueprint> BuildBlueprints(
        string packageDirectory, SeedManifest manifest, Guid versionId, Dictionary<string, Unit> unitsByCode)
    {
        var blueprints = new List<AssessmentBlueprint>();

        foreach (var doc in SeedPackageReader.ReadAll<SeedBlueprintsDoc>(packageDirectory, manifest.Files.Blueprints))
        {
            foreach (var seed in doc.Blueprints)
            {
                var unitId = seed.Unit is null
                    ? (Guid?)null
                    : Resolve(unitsByCode, seed.Unit, "unit", $"{seed.Kind}@{seed.Level}").Id;
                var blueprint = AssessmentBlueprint.Create(versionId, EnumText.Read<BlueprintKind>(seed.Kind), Cefr(seed.Level), unitId);
                foreach (var section in seed.Sections)
                {
                    blueprint.AddSection(EnumText.Read<Skill>(section.Skill), section.ItemCount, section.PassPercentage);
                }

                blueprints.Add(blueprint);
            }
        }

        return blueprints;
    }

    private static CefrLevel Cefr(string text) =>
        CefrLevel.TryParse(text, out var level)
            ? level
            : throw new InvalidDataException($"Invalid CEFR level '{text}' in seed package.");

    private static TValue Resolve<TValue>(Dictionary<string, TValue> lookup, string key, string kind, string context) =>
        lookup.TryGetValue(key, out var value)
            ? value
            : throw new InvalidDataException($"Unresolved {kind} reference '{key}' (from '{context}').");

    private static TValue Require<TValue>(Result<TValue> result, string context) =>
        result.IsSuccess
            ? result.Value
            : throw new InvalidDataException($"Seed rejected by domain — {context}: {result.Error.Code}.");

    private static void Require(Result result, string context)
    {
        if (result.IsFailure)
        {
            throw new InvalidDataException($"Seed rejected by domain — {context}: {result.Error.Code}.");
        }
    }
}
