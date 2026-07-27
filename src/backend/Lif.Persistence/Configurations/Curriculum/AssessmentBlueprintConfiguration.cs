using Lif.Domain.Curriculum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lif.Persistence.Configurations.Curriculum;

/// <summary>curriculum.assessment_blueprints; sections as jsonb snapshot (doc 07 §2, §9).</summary>
public sealed class AssessmentBlueprintConfiguration : IEntityTypeConfiguration<AssessmentBlueprint>
{
    public void Configure(EntityTypeBuilder<AssessmentBlueprint> builder)
    {
        builder.ToTable("assessment_blueprints", "curriculum");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Kind).HasConversion(Converters.EnumToSnakeCaseText<BlueprintKind>());

        builder.Property(b => b.Level)
            .HasConversion(Converters.CefrLevelToText)
            .HasColumnName("cefr_level")
            .HasMaxLength(4);

        builder.Property(b => b.Sections)
            .HasField("_sections")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                JsonColumn.Converter<IReadOnlyList<BlueprintSection>>(),
                JsonColumn.Comparer<IReadOnlyList<BlueprintSection>>())
            .HasColumnType("jsonb");

        builder.HasIndex(b => new { b.VersionId, b.Kind, b.Level });
    }
}
