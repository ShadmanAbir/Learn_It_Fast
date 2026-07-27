using Lif.Domain.Curriculum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lif.Persistence.Configurations.Curriculum;

/// <summary>curriculum.curriculum_versions (doc 07 §2).</summary>
public sealed class CurriculumVersionConfiguration : IEntityTypeConfiguration<CurriculumVersion>
{
    public void Configure(EntityTypeBuilder<CurriculumVersion> builder)
    {
        builder.ToTable("curriculum_versions", "curriculum");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.Language)
            .HasConversion(Converters.LanguageCodeToText)
            .HasColumnName("language_code")
            .HasMaxLength(2);

        builder.Property(v => v.Version).HasMaxLength(32);
        builder.Property(v => v.Status).HasConversion(Converters.EnumToSnakeCaseText<CurriculumVersionStatus>());

        builder.HasIndex(v => new { v.Language, v.Version }).IsUnique();
    }
}
