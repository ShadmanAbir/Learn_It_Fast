using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lif.Persistence;

/// <summary>Design-time factory for `dotnet ef` (migrations pipeline); never used at runtime.</summary>
public sealed class LifDbContextFactory : IDesignTimeDbContextFactory<LifDbContext>
{
    public LifDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LifDbContext>()
            .UseNpgsql("Host=localhost;Database=lif;Username=lif;Password=lif")
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new AuditTimestampInterceptor())
            .Options;

        return new LifDbContext(options);
    }
}
