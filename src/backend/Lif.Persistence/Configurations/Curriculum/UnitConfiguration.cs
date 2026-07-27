using Lif.Domain.Curriculum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lif.Persistence.Configurations.Curriculum;

/// <summary>curriculum.units + curriculum.lesson_templates (doc 07 §2).</summary>
public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("units", "curriculum");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Level)
            .HasConversion(Converters.CefrLevelToText)
            .HasColumnName("cefr_level")
            .HasMaxLength(4);

        builder.HasIndex(u => new { u.VersionId, u.Level, u.OrderIndex }).IsUnique();

        builder.HasMany(u => u.LessonTemplates)
            .WithOne()
            .HasForeignKey(t => t.UnitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.LessonTemplates)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Child of Unit: ordered teach flow over 1–3 concepts (uuid[] per doc 07).</summary>
public sealed class LessonTemplateConfiguration : IEntityTypeConfiguration<LessonTemplate>
{
    public void Configure(EntityTypeBuilder<LessonTemplate> builder)
    {
        builder.ToTable("lesson_templates", "curriculum");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property<List<Guid>>("_conceptIds")
            .HasColumnName("concept_ids");

        builder.HasIndex(t => new { t.UnitId, t.OrderIndex }).IsUnique();
    }
}
