using Lif.Domain.Curriculum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lif.Persistence.Configurations.Curriculum;

/// <summary>
/// curriculum.concepts + owned child tables concept_dependencies / concept_examples /
/// concept_common_mistakes (doc 07 §2). Required vocabulary is kept as uuid[] instead of the
/// doc's join table — same query surface at Phase 1 scale, one table less to maintain.
/// </summary>
public sealed class ConceptConfiguration : IEntityTypeConfiguration<Concept>
{
    public void Configure(EntityTypeBuilder<Concept> builder)
    {
        builder.ToTable("concepts", "curriculum");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        // Computed view over _dependencies — not a second navigation.
        builder.Ignore(c => c.Requires);

        builder.Property(c => c.Code).HasMaxLength(128);
        builder.Property(c => c.Kind).HasConversion(Converters.EnumToSnakeCaseText<ConceptKind>());

        builder.Property(c => c.Level)
            .HasConversion(Converters.CefrLevelToText)
            .HasColumnName("cefr_level")
            .HasMaxLength(4);

        builder.Property(c => c.Difficulty).HasConversion(Converters.DifficultyToSmallint);
        builder.Property(c => c.ExplanationMarkdown).HasColumnName("explanation_md");

        builder.Property<List<Guid>>("_requiredVocabularyIds")
            .HasColumnName("required_vocab_ids");

        builder.HasIndex(c => new { c.VersionId, c.Code }).IsUnique();
        builder.HasIndex(c => c.UnitId);

        builder.OwnsMany(c => c.Dependencies, dependency =>
        {
            dependency.ToTable("concept_dependencies", "curriculum");
            dependency.WithOwner().HasForeignKey("ConceptId");
            dependency.HasKey("ConceptId", nameof(ConceptDependency.PrerequisiteId));
            dependency.Property(d => d.Type)
                .HasConversion(Converters.EnumToSnakeCaseText<DependencyType>())
                .HasColumnName("dep_type");
        });

        builder.OwnsMany(c => c.Examples, example =>
        {
            example.ToTable("concept_examples", "curriculum");
            example.WithOwner().HasForeignKey("ConceptId");
            example.HasKey("ConceptId", nameof(ConceptExample.OrderIndex));
            // Npgsql conventions would make the int PK part an identity column; the domain assigns it.
            example.Property(e => e.OrderIndex).ValueGeneratedNever();
        });

        builder.OwnsMany(c => c.CommonMistakes, mistake =>
        {
            mistake.ToTable("concept_common_mistakes", "curriculum");
            mistake.WithOwner().HasForeignKey("ConceptId");
            mistake.Property<int>("Id");
            mistake.HasKey("ConceptId", "Id");
            mistake.Property(m => m.ErrorCode).HasMaxLength(128);
        });

        builder.Navigation(c => c.Dependencies).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(c => c.Examples).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(c => c.CommonMistakes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
