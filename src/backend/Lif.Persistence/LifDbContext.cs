using Lif.Domain.Curriculum;
using Lif.Domain.Learner;
using Lif.Domain.Learning;
using Lif.Domain.Scheduling;
using Lif.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lif.Persistence;

/// <summary>
/// Single DbContext over the 9 PostgreSQL schemas (doc 07). Entity configurations are added per
/// bounded context under Configurations/&lt;Context&gt;/ as aggregates are implemented (Phase 1+).
/// Global query filters enforce per-learner isolation (NFR-SEC-3).
/// </summary>
public sealed class LifDbContext : DbContext
{
    /// <summary>Shadow audit properties present on every non-owned entity (doc 07 conventions).</summary>
    public const string CreatedAtProperty = "CreatedAt";
    public const string UpdatedAtProperty = "UpdatedAt";

    public LifDbContext(DbContextOptions<LifDbContext> options) : base(options)
    {
    }

    // curriculum
    public DbSet<CurriculumVersion> CurriculumVersions => Set<CurriculumVersion>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Concept> Concepts => Set<Concept>();
    public DbSet<VocabEntry> VocabEntries => Set<VocabEntry>();
    public DbSet<CuratedExercise> CuratedExercises => Set<CuratedExercise>();
    public DbSet<AssessmentBlueprint> AssessmentBlueprints => Set<AssessmentBlueprint>();

    // learner
    public DbSet<LearnerProfile> LearnerProfiles => Set<LearnerProfile>();
    public DbSet<ConceptMastery> ConceptMasteries => Set<ConceptMastery>();
    public DbSet<WordState> WordStates => Set<WordState>();
    public DbSet<MistakeRecord> Mistakes => Set<MistakeRecord>();

    // learning
    public DbSet<StudySession> StudySessions => Set<StudySession>();

    // scheduling
    public DbSet<ReviewItem> ReviewItems => Set<ReviewItem>();
    public DbSet<ReviewLog> ReviewLogs => Set<ReviewLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LifDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned())
            {
                continue;
            }

            // Domain events are dispatched via outbox, never persisted on the aggregate row.
            if (typeof(AggregateRoot).IsAssignableFrom(entityType.ClrType))
            {
                entityType.AddIgnored(nameof(AggregateRoot.DomainEvents));
            }

            // created_at / updated_at on every table; values set by AuditTimestampInterceptor.
            entityType.AddProperty(CreatedAtProperty, typeof(DateTimeOffset));
            entityType.AddProperty(UpdatedAtProperty, typeof(DateTimeOffset));
        }
    }
}
