using Lif.Domain.Curriculum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lif.Persistence.Configurations.Curriculum;

/// <summary>curriculum.vocab_entries + curriculum.word_senses (doc 07 §2).</summary>
public sealed class VocabEntryConfiguration : IEntityTypeConfiguration<VocabEntry>
{
    public void Configure(EntityTypeBuilder<VocabEntry> builder)
    {
        builder.ToTable("vocab_entries", "curriculum");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.PartOfSpeech).HasColumnName("pos").HasMaxLength(16);

        builder.Property(v => v.Level)
            .HasConversion(Converters.CefrLevelToText)
            .HasColumnName("cefr_level")
            .HasMaxLength(4);

        builder.Property(v => v.Difficulty).HasConversion(Converters.DifficultyToSmallint);
        builder.Property(v => v.Gender).HasMaxLength(3);

        builder.HasIndex(v => new { v.VersionId, v.Lemma, v.PartOfSpeech }).IsUnique();
        builder.HasIndex(v => new { v.VersionId, v.FrequencyRank });

        builder.HasMany(v => v.Senses)
            .WithOne()
            .HasForeignKey(s => s.VocabEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(v => v.Senses).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>Child of VocabEntry: one meaning with contextual example.</summary>
public sealed class WordSenseConfiguration : IEntityTypeConfiguration<WordSense>
{
    public void Configure(EntityTypeBuilder<WordSense> builder)
    {
        builder.ToTable("word_senses", "curriculum");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Topic).HasMaxLength(64);

        builder.HasIndex(s => new { s.VocabEntryId, s.SenseOrder }).IsUnique();
    }
}
