using Lif.Application.Abstractions;
using Lif.Domain.Curriculum;
using Lif.Domain.Learner;
using Lif.Domain.Learning;
using Lif.Domain.Scheduling;
using Lif.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Lif.Persistence.Repositories;

/// <summary>EF Core adapters for the Application ports. Change tracking makes the "mutate
/// aggregate in place, then SaveChanges" contract of the handlers work unmodified.</summary>
public sealed class EfUnitOfWork(LifDbContext context) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}

public sealed class CurriculumReader(LifDbContext context) : ICurriculumReader
{
    public Task<CurriculumVersion?> GetActiveVersionAsync(
        LanguageCode language, CancellationToken cancellationToken = default) =>
        context.CurriculumVersions
            .Where(v => v.Language == language && v.Status == CurriculumVersionStatus.Published)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Concept>> GetConceptsAsync(
        Guid versionId, CancellationToken cancellationToken = default) =>
        await context.Concepts
            .AsNoTracking()
            .Where(c => c.VersionId == versionId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> GetUnitOrderAsync(
        Guid versionId, CancellationToken cancellationToken = default) =>
        await context.Units
            .AsNoTracking()
            .Where(u => u.VersionId == versionId)
            .Select(u => new { u.Id, u.OrderIndex })
            .ToDictionaryAsync(u => u.Id, u => u.OrderIndex, cancellationToken);

    public async Task<IReadOnlyList<CuratedExercise>> GetExercisesForConceptsAsync(
        Guid versionId, IReadOnlyCollection<Guid> conceptIds, CancellationToken cancellationToken = default)
    {
        // ConceptIds live inside the spec jsonb; filter client-side (Phase 1 bank: tens of rows/version).
        var bank = await context.CuratedExercises
            .AsNoTracking()
            .Where(e => e.VersionId == versionId)
            .ToListAsync(cancellationToken);

        return bank.Where(e => e.Spec.ConceptIds.Any(conceptIds.Contains)).ToList();
    }
}

public sealed class LearnerRepository(LifDbContext context) : ILearnerRepository
{
    public Task<LearnerProfile?> GetAsync(Guid learnerId, CancellationToken cancellationToken = default) =>
        context.LearnerProfiles.FirstOrDefaultAsync(p => p.Id == learnerId, cancellationToken);

    public void Add(LearnerProfile profile) => context.LearnerProfiles.Add(profile);
}

public sealed class ConceptMasteryRepository(LifDbContext context) : IConceptMasteryRepository
{
    public async Task<IReadOnlyList<ConceptMastery>> GetAllAsync(
        Guid learnerId, CancellationToken cancellationToken = default) =>
        await context.ConceptMasteries
            .Where(m => m.LearnerId == learnerId)
            .ToListAsync(cancellationToken);

    public Task<ConceptMastery?> GetAsync(
        Guid learnerId, Guid conceptId, CancellationToken cancellationToken = default) =>
        context.ConceptMasteries.FirstOrDefaultAsync(
            m => m.LearnerId == learnerId && m.ConceptId == conceptId, cancellationToken);

    public void Add(ConceptMastery mastery) => context.ConceptMasteries.Add(mastery);
}

public sealed class WordStateRepository(LifDbContext context) : IWordStateRepository
{
    public Task<WordState?> GetAsync(
        Guid learnerId, Guid vocabEntryId, CancellationToken cancellationToken = default) =>
        context.WordStates.FirstOrDefaultAsync(
            w => w.LearnerId == learnerId && w.VocabEntryId == vocabEntryId, cancellationToken);

    public void Add(WordState wordState) => context.WordStates.Add(wordState);
}

public sealed class MistakeRepository(LifDbContext context) : IMistakeRepository
{
    public void Add(MistakeRecord mistake) => context.Mistakes.Add(mistake);
}

public sealed class StudySessionRepository(LifDbContext context) : IStudySessionRepository
{
    public Task<StudySession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        Aggregates().FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

    public Task<StudySession?> GetOpenForLearnerAsync(
        Guid learnerId, CancellationToken cancellationToken = default) =>
        Aggregates()
            .Where(s => s.LearnerId == learnerId
                && (s.Status == SessionStatus.Planned || s.Status == SessionStatus.Active))
            .OrderByDescending(s => s.Id) // v7 guids are time-ordered
            .FirstOrDefaultAsync(cancellationToken);

    public Task<StudySession?> GetByExerciseAsync(
        Guid exerciseInstanceId, CancellationToken cancellationToken = default) =>
        Aggregates().FirstOrDefaultAsync(
            s => s.Activities.Any(a => a.Exercises.Any(e => e.Id == exerciseInstanceId)),
            cancellationToken);

    public void Add(StudySession session) => context.StudySessions.Add(session);

    private IQueryable<StudySession> Aggregates() =>
        context.StudySessions
            .Include(s => s.Activities)
            .ThenInclude(a => a.Exercises)
            .ThenInclude(e => e.Attempts);
}

public sealed class ReviewItemRepository(LifDbContext context) : IReviewItemRepository
{
    public Task<ReviewItem?> GetAsync(Guid reviewItemId, CancellationToken cancellationToken = default) =>
        context.ReviewItems.FirstOrDefaultAsync(r => r.Id == reviewItemId, cancellationToken);

    public Task<ReviewItem?> FindAsync(
        Guid learnerId, ReviewItemKind kind, Guid itemRef, CancellationToken cancellationToken = default) =>
        context.ReviewItems.FirstOrDefaultAsync(
            r => r.LearnerId == learnerId && r.Kind == kind && r.ItemRef == itemRef, cancellationToken);

    public Task<int> CountDueAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        context.ReviewItems.CountAsync(
            r => r.LearnerId == learnerId && r.State.DueAt <= now, cancellationToken);

    public async Task<IReadOnlyList<ReviewItem>> GetDueAsync(
        Guid learnerId, DateTimeOffset now, int limit, CancellationToken cancellationToken = default) =>
        await context.ReviewItems
            .Where(r => r.LearnerId == learnerId && r.State.DueAt <= now)
            .OrderBy(r => r.State.DueAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public void Add(ReviewItem item) => context.ReviewItems.Add(item);

    public void AddLog(ReviewLog log) => context.ReviewLogs.Add(log);
}
