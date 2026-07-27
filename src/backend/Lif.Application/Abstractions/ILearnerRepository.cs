using Lif.Domain.Learner;

namespace Lif.Application.Abstractions;

/// <summary>Learner profile aggregate store.</summary>
public interface ILearnerRepository
{
    public Task<LearnerProfile?> GetAsync(Guid learnerId, CancellationToken cancellationToken = default);

    public void Add(LearnerProfile profile);
}
