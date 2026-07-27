namespace Lif.Application.Abstractions;

/// <summary>Testable clock; domain services never call DateTimeOffset.Now directly (determinism for simulation).</summary>
public interface IClock
{
    public DateTimeOffset UtcNow { get; }
}
