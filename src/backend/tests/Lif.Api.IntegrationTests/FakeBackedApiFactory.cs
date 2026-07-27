using Lif.Application.Abstractions;
using Lif.Application.Tests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lif.Api.IntegrationTests;

/// <summary>
/// Boots the real host with the Application-layer fakes swapped in for the EF ports:
/// full HTTP surface (routing, binding, problem+json) without PostgreSQL. Database-backed
/// round-trips run in the Testcontainers suite (CI).
/// </summary>
public sealed class FakeBackedApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Seeded in-memory world: tiny A1 curriculum + all port fakes.</summary>
    public TestWorld World { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // No connection string in tests → Program skipped ports + handlers; wire them here.
            services.AddSingleton<IClock>(World.Clock);
            services.AddSingleton<IUnitOfWork>(World.UnitOfWork);
            services.AddSingleton<ICurriculumReader>(World.Curriculum);
            services.AddSingleton<ILearnerRepository>(World.Learners);
            services.AddSingleton<IConceptMasteryRepository>(World.Masteries);
            services.AddSingleton<IWordStateRepository>(World.WordStates);
            services.AddSingleton<IMistakeRepository>(World.Mistakes);
            services.AddSingleton<IStudySessionRepository>(World.Sessions);
            services.AddSingleton<IReviewItemRepository>(World.Reviews);
            services.AddUseCaseHandlers();
        });
    }
}
