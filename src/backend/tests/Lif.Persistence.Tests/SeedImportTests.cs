using Lif.Domain.Curriculum;
using Lif.Persistence.Seeding;

namespace Lif.Persistence.Tests;

/// <summary>
/// Runs the importer's pure resolution stage against the real de-curriculum seed package —
/// every cross-file reference (unit, concept, vocab, exercise) must resolve through domain
/// factories. Database round-trips run in the Testcontainers suite.
/// </summary>
public class SeedImportTests
{
    private static readonly CurriculumSeedModel Model = CurriculumSeedImporter.Build(FindPackageDirectory());

    private static string FindPackageDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "seeds", "curriculum", "de");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate seeds/curriculum/de above the test output directory.");
    }

    [Fact]
    public void Version_is_published_de_package()
    {
        Model.Version.Language.Value.Should().Be("de");
        Model.Version.Version.Should().Be("0.1.0");
        Model.Version.Status.Should().Be(CurriculumVersionStatus.Published);
    }

    [Fact]
    public void A1_1_slice_has_expected_content_counts()
    {
        Model.Units.Should().HaveCount(2);
        Model.Units.SelectMany(u => u.LessonTemplates).Should().HaveCount(8);
        Model.Concepts.Should().HaveCount(15);
        Model.VocabEntries.Should().HaveCount(127);
        Model.Exercises.Should().HaveCount(41);
        Model.Blueprints.Should().HaveCount(1);
    }

    [Fact]
    public void Every_concept_is_assigned_to_a_seeded_unit()
    {
        var unitIds = Model.Units.Select(u => u.Id).ToHashSet();

        Model.Concepts.Should().AllSatisfy(c =>
        {
            c.UnitId.Should().NotBeNull();
            unitIds.Should().Contain(c.UnitId!.Value);
        });
    }

    [Fact]
    public void Dependency_edges_resolve_to_seeded_concepts()
    {
        var conceptIds = Model.Concepts.Select(c => c.Id).ToHashSet();

        Model.Concepts.SelectMany(c => c.Dependencies).Should().NotBeEmpty();
        Model.Concepts.SelectMany(c => c.Dependencies)
            .Should().AllSatisfy(d => conceptIds.Should().Contain(d.PrerequisiteId));
    }

    [Fact]
    public void Required_vocabulary_resolves_to_seeded_entries()
    {
        var vocabIds = Model.VocabEntries.Select(v => v.Id).ToHashSet();

        Model.Concepts.Should().AllSatisfy(c => c.RequiredVocabularyIds.Should().NotBeEmpty());
        Model.Concepts.SelectMany(c => c.RequiredVocabularyIds)
            .Should().AllSatisfy(id => vocabIds.Should().Contain(id));
    }

    [Fact]
    public void Lesson_templates_reference_seeded_concepts()
    {
        var conceptIds = Model.Concepts.Select(c => c.Id).ToHashSet();

        Model.Units.SelectMany(u => u.LessonTemplates)
            .Should().AllSatisfy(t =>
            {
                t.ConceptIds.Should().NotBeEmpty();
                t.ConceptIds.Should().AllSatisfy(id => conceptIds.Should().Contain(id));
            });
    }

    [Fact]
    public void Exercises_cover_seeded_concepts_with_valid_specs()
    {
        var conceptIds = Model.Concepts.Select(c => c.Id).ToHashSet();

        Model.Exercises.Should().AllSatisfy(e =>
        {
            e.Spec.ConceptIds.Should().NotBeEmpty();
            e.Spec.ConceptIds.Should().AllSatisfy(id => conceptIds.Should().Contain(id));
            e.Spec.Stem.Should().NotBeNullOrWhiteSpace();
            e.Spec.AnswerKey.Should().NotBeNullOrWhiteSpace();
        });

        // Every concept has at least one curated fallback exercise (ADR-008).
        var covered = Model.Exercises.SelectMany(e => e.Spec.ConceptIds).ToHashSet();
        Model.Concepts.Should().AllSatisfy(c => covered.Should().Contain(c.Id));
    }

    [Fact]
    public void Checkpoint_blueprint_targets_unit_two()
    {
        var blueprint = Model.Blueprints.Single();

        blueprint.Kind.Should().Be(BlueprintKind.Checkpoint);
        blueprint.UnitId.Should().Be(Model.Units.Single(u => u.OrderIndex == 1).Id);
        blueprint.Sections.Should().HaveCount(3);
        blueprint.Sections.Sum(s => s.ItemCount).Should().Be(20);
    }

    [Fact]
    public void Case_sensitive_vocab_references_stay_distinct()
    {
        // "sie" (she/they) and "Sie" (formal you) are different lexicon entries.
        Model.VocabEntries.Count(v => v.Lemma is "sie" or "Sie").Should().Be(2);
    }
}
