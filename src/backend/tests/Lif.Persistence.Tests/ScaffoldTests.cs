namespace Lif.Persistence.Tests;

/// <summary>Placeholder; Testcontainers-backed mapping/repository tests arrive with the first aggregates (Phase 1).</summary>
public class ScaffoldTests
{
    [Fact]
    public void PersistenceAssembly_IsDiscoverable()
    {
        typeof(Lif.Persistence.LifDbContext).Assembly.Should().NotBeNull();
    }
}
