namespace Lif.Application.Abstractions;

/// <summary>Commits all aggregate changes of one use case atomically (implemented by Persistence).</summary>
public interface IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
