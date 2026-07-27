using Lif.Domain.Learner;

namespace Lif.Application.Abstractions;

/// <summary>Per-learner concept mastery store.</summary>
public interface IConceptMasteryRepository
{
    /// <summary>All mastery rows for a learner (frontier input; missing concept = mastery 0).</summary>
    public Task<IReadOnlyList<ConceptMastery>> GetAllAsync(Guid learnerId, CancellationToken cancellationToken = default);

    public Task<ConceptMastery?> GetAsync(Guid learnerId, Guid conceptId, CancellationToken cancellationToken = default);

    public void Add(ConceptMastery mastery);
}
