using Lif.Application.Abstractions;

namespace Lif.Infrastructure.Time;

/// <summary>Production clock; tests substitute IClock with a controllable fake.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
