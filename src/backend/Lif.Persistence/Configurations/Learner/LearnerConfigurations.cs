using Lif.Domain.Learner;
using Lif.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lif.Persistence.Configurations.Learner;

/// <summary>learner.profiles (doc 07 §3).</summary>
public sealed class LearnerProfileConfiguration : IEntityTypeConfiguration<LearnerProfile>
{
    public void Configure(EntityTypeBuilder<LearnerProfile> builder)
    {
        builder.ToTable("profiles", "learner");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.UiLanguage)
            .HasConversion(Converters.LanguageCodeToText)
            .HasMaxLength(2);

        builder.Property(p => p.TargetLevel)
            .HasConversion(Converters.CefrLevelToText)
            .HasColumnName("target_cefr")
            .HasMaxLength(4);

        builder.Property(p => p.CurrentLevel)
            .HasConversion(Converters.CefrLevelToText)
            .HasColumnName("current_cefr")
            .HasMaxLength(4);

        builder.Property(p => p.MotivationType).HasMaxLength(32);
    }
}

/// <summary>learner.concept_mastery — BKT state per (learner, concept) (doc 07 §3).</summary>
public sealed class ConceptMasteryConfiguration : IEntityTypeConfiguration<ConceptMastery>
{
    public void Configure(EntityTypeBuilder<ConceptMastery> builder)
    {
        builder.ToTable("concept_mastery", "learner");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.HasIndex(m => new { m.LearnerId, m.ConceptId }).IsUnique();
    }
}

/// <summary>learner.word_states — FSRS state lives on scheduling.review_items, not here (doc 07 §3).</summary>
public sealed class WordStateConfiguration : IEntityTypeConfiguration<WordState>
{
    public void Configure(EntityTypeBuilder<WordState> builder)
    {
        builder.ToTable("word_states", "learner");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.Status).HasConversion(Converters.EnumToSnakeCaseText<WordStatus>());
        builder.Property(w => w.Source).HasConversion(Converters.EnumToSnakeCaseText<WordSource>());

        builder.HasIndex(w => new { w.LearnerId, w.VocabEntryId }).IsUnique();
        builder.HasIndex(w => new { w.LearnerId, w.Status });
    }
}

/// <summary>learner.mistakes — append-only error stream (doc 07 §3).</summary>
public sealed class MistakeRecordConfiguration : IEntityTypeConfiguration<MistakeRecord>
{
    public void Configure(EntityTypeBuilder<MistakeRecord> builder)
    {
        builder.ToTable("mistakes", "learner");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Skill).HasConversion(Converters.EnumToSnakeCaseText<Skill>());
        builder.Property(m => m.SourceKind).HasConversion(Converters.EnumToSnakeCaseText<MistakeSource>());
        builder.Property(m => m.ErrorCode).HasMaxLength(128);

        builder.HasIndex(m => new { m.LearnerId, m.OccurredAt }).IsDescending(false, true);
        builder.HasIndex(m => new { m.LearnerId, m.ConceptId });
    }
}
