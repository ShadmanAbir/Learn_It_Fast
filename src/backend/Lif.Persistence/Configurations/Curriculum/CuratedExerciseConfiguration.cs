using Lif.Domain.Curriculum;
using Lif.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lif.Persistence.Configurations.Curriculum;

/// <summary>
/// curriculum.exercises — the curated exercise bank (ADR-008). Not in the doc 07 DDL, which
/// predates the bank; spec is a write-once jsonb snapshot like learning.exercise_instances.spec.
/// </summary>
public sealed class CuratedExerciseConfiguration : IEntityTypeConfiguration<CuratedExercise>
{
    public void Configure(EntityTypeBuilder<CuratedExercise> builder)
    {
        builder.ToTable("exercises", "curriculum");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Code).HasMaxLength(128);

        builder.Property(e => e.Spec)
            .HasConversion(JsonColumn.Converter<ExerciseSpec>(), JsonColumn.Comparer<ExerciseSpec>())
            .HasColumnType("jsonb");

        builder.HasIndex(e => new { e.VersionId, e.Code }).IsUnique();
    }
}
