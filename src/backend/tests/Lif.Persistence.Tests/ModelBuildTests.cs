using Lif.Domain.Curriculum;
using Lif.Domain.Learner;
using Lif.Domain.Learning;
using Lif.Domain.Scheduling;
using Lif.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Lif.Persistence.Tests;

/// <summary>
/// Finalizes and validates the full EF model without a database connection — catches broken
/// mappings (unmappable navigations, missing keys, bad backing fields) at unit-test speed.
/// Schema round-trips against real PostgreSQL run in the Testcontainers suite.
/// </summary>
public class ModelBuildTests
{
    private static LifDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LifDbContext>()
            .UseNpgsql("Host=model-validation-only")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new LifDbContext(options);
    }

    [Fact]
    public void Model_builds_and_validates()
    {
        using var context = CreateContext();

        var act = () => _ = context.Model;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(typeof(CurriculumVersion), "curriculum", "curriculum_versions")]
    [InlineData(typeof(Unit), "curriculum", "units")]
    [InlineData(typeof(LessonTemplate), "curriculum", "lesson_templates")]
    [InlineData(typeof(Concept), "curriculum", "concepts")]
    [InlineData(typeof(VocabEntry), "curriculum", "vocab_entries")]
    [InlineData(typeof(WordSense), "curriculum", "word_senses")]
    [InlineData(typeof(CuratedExercise), "curriculum", "exercises")]
    [InlineData(typeof(AssessmentBlueprint), "curriculum", "assessment_blueprints")]
    [InlineData(typeof(LearnerProfile), "learner", "profiles")]
    [InlineData(typeof(ConceptMastery), "learner", "concept_mastery")]
    [InlineData(typeof(WordState), "learner", "word_states")]
    [InlineData(typeof(MistakeRecord), "learner", "mistakes")]
    [InlineData(typeof(StudySession), "learning", "sessions")]
    [InlineData(typeof(Activity), "learning", "activities")]
    [InlineData(typeof(ExerciseInstance), "learning", "exercise_instances")]
    [InlineData(typeof(Attempt), "learning", "attempts")]
    [InlineData(typeof(ReviewItem), "scheduling", "review_items")]
    [InlineData(typeof(ReviewLog), "scheduling", "review_logs")]
    public void Entities_map_to_doc07_tables(Type clrType, string schema, string table)
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(clrType);

        entityType.Should().NotBeNull();
        entityType!.GetSchema().Should().Be(schema);
        entityType.GetTableName().Should().Be(table);
    }

    [Fact]
    public void Every_table_carries_audit_timestamps()
    {
        using var context = CreateContext();

        foreach (var entityType in context.Model.GetEntityTypes().Where(t => !t.IsOwned()))
        {
            entityType.FindProperty(LifDbContext.CreatedAtProperty).Should().NotBeNull(
                $"{entityType.DisplayName()} must have created_at");
            entityType.FindProperty(LifDbContext.UpdatedAtProperty).Should().NotBeNull(
                $"{entityType.DisplayName()} must have updated_at");
        }
    }

    [Fact]
    public void Aggregate_roots_do_not_persist_domain_events()
    {
        using var context = CreateContext();

        foreach (var entityType in context.Model.GetEntityTypes().Where(t => !t.IsOwned()))
        {
            entityType.FindProperty("DomainEvents").Should().BeNull();
            entityType.GetNavigations().Should().NotContain(n => n.Name == "DomainEvents");
        }
    }

    [Fact]
    public void Fsrs_state_flattens_to_doc07_columns()
    {
        using var context = CreateContext();

        var owned = context.Model.FindEntityType(typeof(FsrsState));

        owned.Should().NotBeNull();
        var columns = owned!.GetProperties()
            .Select(p => p.GetColumnName())
            .Where(c => c is not null)
            .ToList();
        columns.Should().Contain(["fsrs_stability", "fsrs_difficulty", "last_review_at", "due_at"]);
    }

    [Fact]
    public void Exercise_spec_round_trips_through_jsonb_converter()
    {
        var spec = new ExerciseSpec(
            ExerciseType.Ordering,
            "Bilde einen Satz. Beginne mit 'Heute'.",
            ["ich", "Deutsch", "Heute", "lerne"],
            "Heute|lerne|ich|Deutsch",
            [Guid.CreateVersion7()],
            Difficulty.From(2),
            Explanation: null,
            ErrorCode: "err.wordorder.v2");

        var converter = JsonColumn.Converter<ExerciseSpec>();
        var json = (string)converter.ConvertToProvider(spec)!;
        var restored = (ExerciseSpec)converter.ConvertFromProvider(json)!;

        restored.Type.Should().Be(spec.Type);
        restored.Stem.Should().Be(spec.Stem);
        restored.Options.Should().Equal(spec.Options);
        restored.AnswerKey.Should().Be(spec.AnswerKey);
        restored.ConceptIds.Should().Equal(spec.ConceptIds);
        restored.Difficulty.Should().Be(spec.Difficulty);
        restored.Explanation.Should().BeNull();
        restored.ErrorCode.Should().Be(spec.ErrorCode);
    }

    [Fact]
    public void Session_plan_round_trips_through_jsonb_converter()
    {
        var plan = new SessionPlan(
            0.4, 0.4, 0.2,
            [new ActivityPlan(ActivityKind.Review, 5, [Guid.CreateVersion7()], 10)],
            ["review debt first"],
            Seed: 42,
            PlannerVersion: "1.0.0");

        var converter = JsonColumn.Converter<SessionPlan>();
        var restored = (SessionPlan)converter.ConvertFromProvider(converter.ConvertToProvider(plan))!;

        restored.ReviewShare.Should().Be(plan.ReviewShare);
        restored.Activities.Should().HaveCount(1);
        restored.Activities[0].Kind.Should().Be(ActivityKind.Review);
        restored.Activities[0].ConceptIds.Should().Equal(plan.Activities[0].ConceptIds);
        restored.Rationale.Should().Equal(plan.Rationale);
        restored.Seed.Should().Be(42);
        restored.PlannerVersion.Should().Be("1.0.0");
    }

    [Theory]
    [InlineData(BlueprintKind.LevelUp, "level_up")]
    [InlineData(ExerciseSource.AiGenerated, "ai_generated")]
    [InlineData(ExerciseType.MultipleChoice, "multiple_choice")]
    [InlineData(ReviewItemKind.ConceptDrill, "concept_drill")]
    [InlineData(WordStatus.Relearning, "relearning")]
    public void Enums_store_as_doc07_snake_case_text(object value, string expected)
    {
        var written = value switch
        {
            BlueprintKind kind => EnumText.Write(kind),
            ExerciseSource source => EnumText.Write(source),
            ExerciseType type => EnumText.Write(type),
            ReviewItemKind kind => EnumText.Write(kind),
            WordStatus status => EnumText.Write(status),
            _ => throw new InvalidOperationException("unhandled enum type"),
        };

        written.Should().Be(expected);
        EnumText.Read<BlueprintKind>("level_up").Should().Be(BlueprintKind.LevelUp);
    }
}
