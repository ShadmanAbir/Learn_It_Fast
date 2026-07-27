using Lif.SharedKernel.Domain;

namespace Lif.Domain.Learner;

/// <summary>Raised whenever a recorded observation moves a learner's concept mastery.</summary>
public sealed record ConceptMasteryChanged(
    Guid LearnerId,
    Guid ConceptId,
    double PreviousMastery,
    double NewMastery,
    DateTimeOffset OccurredAt) : IDomainEvent;
