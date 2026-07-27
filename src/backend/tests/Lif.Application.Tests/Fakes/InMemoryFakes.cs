using Lif.Application.Abstractions;
using Lif.Domain.Curriculum;
using Lif.Domain.Learner;
using Lif.Domain.Learning;
using Lif.Domain.Scheduling;
using Lif.SharedKernel.Primitives;

namespace Lif.Application.Tests.Fakes;

/// <summary>Settable clock for deterministic handler tests.</summary>
public sealed class FixedClock(DateTimeOffset start) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = start;

    public void Advance(TimeSpan delta) => UtcNow += delta;
}

/// <summary>Counts commits; the fakes mutate aggregates in place, so there is nothing to flush.</summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

public sealed class FakeCurriculumReader : ICurriculumReader
{
    public CurriculumVersion? ActiveVersion { get; set; }

    public List<Concept> Concepts { get; } = [];

    public Dictionary<Guid, int> UnitOrder { get; } = [];

    public List<CuratedExercise> Exercises { get; } = [];

    public Task<CurriculumVersion?> GetActiveVersionAsync(LanguageCode language, CancellationToken cancellationToken = default) =>
        Task.FromResult(ActiveVersion?.Language == language ? ActiveVersion : null);

    public Task<IReadOnlyList<Concept>> GetConceptsAsync(Guid versionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Concept>>(Concepts.Where(c => c.VersionId == versionId).ToList());

    public Task<IReadOnlyDictionary<Guid, int>> GetUnitOrderAsync(Guid versionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, int>>(UnitOrder);

    public Task<IReadOnlyList<CuratedExercise>> GetExercisesForConceptsAsync(
        Guid versionId, IReadOnlyCollection<Guid> conceptIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CuratedExercise>>(Exercises
            .Where(e => e.VersionId == versionId && e.Spec.ConceptIds.Any(conceptIds.Contains))
            .ToList());
}

public sealed class FakeLearnerRepository : ILearnerRepository
{
    public List<LearnerProfile> Profiles { get; } = [];

    public Task<LearnerProfile?> GetAsync(Guid learnerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Profiles.FirstOrDefault(p => p.Id == learnerId));

    public void Add(LearnerProfile profile) => Profiles.Add(profile);
}

public sealed class FakeConceptMasteryRepository : IConceptMasteryRepository
{
    public List<ConceptMastery> Rows { get; } = [];

    public Task<IReadOnlyList<ConceptMastery>> GetAllAsync(Guid learnerId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ConceptMastery>>(Rows.Where(m => m.LearnerId == learnerId).ToList());

    public Task<ConceptMastery?> GetAsync(Guid learnerId, Guid conceptId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Rows.FirstOrDefault(m => m.LearnerId == learnerId && m.ConceptId == conceptId));

    public void Add(ConceptMastery mastery) => Rows.Add(mastery);
}

public sealed class FakeWordStateRepository : IWordStateRepository
{
    public List<WordState> Rows { get; } = [];

    public Task<WordState?> GetAsync(Guid learnerId, Guid vocabEntryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Rows.FirstOrDefault(w => w.LearnerId == learnerId && w.VocabEntryId == vocabEntryId));

    public void Add(WordState wordState) => Rows.Add(wordState);
}

public sealed class FakeMistakeRepository : IMistakeRepository
{
    public List<MistakeRecord> Rows { get; } = [];

    public void Add(MistakeRecord mistake) => Rows.Add(mistake);
}

public sealed class FakeStudySessionRepository : IStudySessionRepository
{
    public List<StudySession> Sessions { get; } = [];

    public Task<StudySession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Sessions.FirstOrDefault(s => s.Id == sessionId));

    public Task<StudySession?> GetOpenForLearnerAsync(Guid learnerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Sessions.FirstOrDefault(s =>
            s.LearnerId == learnerId && s.Status is SessionStatus.Planned or SessionStatus.Active));

    public Task<StudySession?> GetByExerciseAsync(Guid exerciseInstanceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Sessions.FirstOrDefault(s =>
            s.Activities.SelectMany(a => a.Exercises).Any(e => e.Id == exerciseInstanceId)));

    public void Add(StudySession session) => Sessions.Add(session);
}

public sealed class FakeReviewItemRepository : IReviewItemRepository
{
    public List<ReviewItem> Items { get; } = [];

    public List<ReviewLog> Logs { get; } = [];

    public Task<ReviewItem?> GetAsync(Guid reviewItemId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(i => i.Id == reviewItemId));

    public Task<ReviewItem?> FindAsync(Guid learnerId, ReviewItemKind kind, Guid itemRef, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(i => i.LearnerId == learnerId && i.Kind == kind && i.ItemRef == itemRef));

    public Task<int> CountDueAsync(Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Count(i => i.LearnerId == learnerId && i.State.IsDue(now)));

    public Task<IReadOnlyList<ReviewItem>> GetDueAsync(Guid learnerId, DateTimeOffset now, int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReviewItem>>(Items
            .Where(i => i.LearnerId == learnerId && i.State.IsDue(now))
            .OrderBy(i => i.State.DueAt)
            .Take(limit)
            .ToList());

    public void Add(ReviewItem item) => Items.Add(item);

    public void AddLog(ReviewLog log) => Logs.Add(log);
}
