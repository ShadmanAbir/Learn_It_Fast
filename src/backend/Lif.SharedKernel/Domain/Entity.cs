namespace Lif.SharedKernel.Domain;

/// <summary>Base class for all entities. Identity is a time-ordered UUIDv7 (ADR: offline-first sync safety).</summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id) => Id = id;

    protected Entity() => Id = Guid.CreateVersion7();

    public Guid Id { get; }

    public bool Equals(Entity? other) =>
        other is not null && other.GetType() == GetType() && other.Id == Id;

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => Id.GetHashCode();
}
