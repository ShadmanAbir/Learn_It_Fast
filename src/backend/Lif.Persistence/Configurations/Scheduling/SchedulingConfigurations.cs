using Lif.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lif.Persistence.Configurations.Scheduling;

/// <summary>
/// scheduling.review_items — the unified review queue. FSRS memory state is owned and
/// flattened to fsrs_stability / fsrs_difficulty / last_review_at / due_at (doc 07 §6).
/// </summary>
public sealed class ReviewItemConfiguration : IEntityTypeConfiguration<ReviewItem>
{
    public void Configure(EntityTypeBuilder<ReviewItem> builder)
    {
        builder.ToTable("review_items", "scheduling");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Kind).HasConversion(Converters.EnumToSnakeCaseText<ReviewItemKind>());

        builder.OwnsOne(r => r.State, state =>
        {
            state.Property(s => s.Stability).HasColumnName("fsrs_stability");
            state.Property(s => s.Difficulty).HasColumnName("fsrs_difficulty");
            state.Property(s => s.LastReviewAt).HasColumnName("last_review_at");
            state.Property(s => s.DueAt).HasColumnName("due_at");
            state.HasIndex(s => s.DueAt);
        });

        builder.Navigation(r => r.State).IsRequired();

        builder.HasIndex(r => new { r.LearnerId, r.Kind, r.ItemRef }).IsUnique();
    }
}

/// <summary>scheduling.review_logs — append-only input for future FSRS optimization (doc 07 §6).</summary>
public sealed class ReviewLogConfiguration : IEntityTypeConfiguration<ReviewLog>
{
    public void Configure(EntityTypeBuilder<ReviewLog> builder)
    {
        builder.ToTable("review_logs", "scheduling");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Rating).HasConversion<short>();

        builder.HasIndex(l => new { l.ReviewItemId, l.ReviewedAt });
        builder.HasIndex(l => new { l.LearnerId, l.ReviewedAt });
    }
}
