using Lif.Domain.Learner;

namespace Lif.Application.Abstractions;

/// <summary>Per-learner word state store.</summary>
public interface IWordStateRepository
{
    public Task<WordState?> GetAsync(Guid learnerId, Guid vocabEntryId, CancellationToken cancellationToken = default);

    public void Add(WordState wordState);
}
