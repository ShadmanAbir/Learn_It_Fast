using Lif.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lif.Persistence.Configurations.Learning;

/// <summary>learning.sessions — plan is a write-once jsonb snapshot (doc 07 §4, §9).</summary>
public sealed class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
{
    public void Configure(EntityTypeBuilder<StudySession> builder)
    {
        builder.ToTable("sessions", "learning");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Status).HasConversion(Converters.EnumToSnakeCaseText<SessionStatus>());

        builder.Property(s => s.Plan)
            .HasConversion(JsonColumn.Converter<SessionPlan>(), JsonColumn.Comparer<SessionPlan>())
            .HasColumnType("jsonb");

        builder.HasIndex(s => new { s.LearnerId, s.Status });

        builder.HasMany(s => s.Activities)
            .WithOne()
            .HasForeignKey(a => a.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Activities).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>learning.activities — one planned block inside a session (doc 07 §4).</summary>
public sealed class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("activities", "learning");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Kind).HasConversion(Converters.EnumToSnakeCaseText<ActivityKind>());
        builder.Property(a => a.Status).HasConversion(Converters.EnumToSnakeCaseText<ActivityStatus>());

        builder.HasIndex(a => new { a.SessionId, a.OrderIndex }).IsUnique();

        builder.HasMany(a => a.Exercises)
            .WithOne()
            .HasForeignKey(e => e.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.Exercises).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>learning.exercise_instances — spec jsonb, provenance, generation meta (doc 07 §4).</summary>
public sealed class ExerciseInstanceConfiguration : IEntityTypeConfiguration<ExerciseInstance>
{
    public void Configure(EntityTypeBuilder<ExerciseInstance> builder)
    {
        builder.ToTable("exercise_instances", "learning");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Spec)
            .HasConversion(JsonColumn.Converter<ExerciseSpec>(), JsonColumn.Comparer<ExerciseSpec>())
            .HasColumnType("jsonb");

        builder.Property(e => e.Source).HasConversion(Converters.EnumToSnakeCaseText<ExerciseSource>());
        builder.Property(e => e.GenerationMeta).HasColumnName("gen_meta").HasColumnType("jsonb");

        builder.HasIndex(e => new { e.ActivityId, e.OrderIndex }).IsUnique();

        builder.HasMany(e => e.Attempts)
            .WithOne()
            .HasForeignKey(a => a.ExerciseInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Attempts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

/// <summary>learning.attempts — append-only (doc 07 §4; monthly partitioning deferred, §9).</summary>
public sealed class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
{
    public void Configure(EntityTypeBuilder<Attempt> builder)
    {
        builder.ToTable("attempts", "learning");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.EvaluatedBy).HasMaxLength(64);

        builder.HasIndex(a => new { a.LearnerId, a.OccurredAt });
    }
}
