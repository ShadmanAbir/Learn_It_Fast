using Lif.Domain.Learning;

namespace Lif.Application.Abstractions;

/// <summary>Study session aggregate store.</summary>
public interface IStudySessionRepository
{
    public Task<StudySession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Latest Planned or Active session for a learner, if any (one open session at a time).</summary>
    public Task<StudySession?> GetOpenForLearnerAsync(Guid learnerId, CancellationToken cancellationToken = default);

    /// <summary>Session owning the given exercise instance (attempt routing, doc 12 §3).</summary>
    public Task<StudySession?> GetByExerciseAsync(Guid exerciseInstanceId, CancellationToken cancellationToken = default);

    public void Add(StudySession session);
}
