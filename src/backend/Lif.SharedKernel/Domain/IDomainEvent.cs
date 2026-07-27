namespace Lif.SharedKernel.Domain;

/// <summary>A domain event raised by an aggregate; dispatched via the transactional outbox (ADR-010).</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
