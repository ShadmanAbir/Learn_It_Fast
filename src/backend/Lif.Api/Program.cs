using Lif.Api;
using Lif.Api.Endpoints;
using Lif.Api.HealthChecks;
using Lif.Application.Abstractions;
using Lif.Application.Ai;
using Lif.Infrastructure.Ai.Orchestration;
using Lif.Infrastructure.Ai.Providers;
using Lif.Infrastructure.Time;
using Lif.Persistence;
using Lif.Persistence.Repositories;
using Lif.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- Logging (Serilog, configured via appsettings "Serilog" section) ---
    // preserveStaticLogger: each host keeps its own logger; the static bootstrap logger is never
    // frozen, so parallel test hosts (WebApplicationFactory) can boot side by side.
    builder.Host.UseSerilog(
        (context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext(),
        preserveStaticLogger: true);

    // --- OpenTelemetry: traces + metrics; console locally, OTLP when endpoint configured (doc 18 §5) ---
    var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("lif", serviceVersion: "0.1.0"))
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
            if (string.IsNullOrEmpty(otlpEndpoint))
            {
                tracing.AddConsoleExporter();
            }
            else
            {
                tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
            }
        })
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation();
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
            }
        });

    // --- Composition root: the only place Infrastructure + Persistence are wired (doc 14 §2 rule 3) ---
    builder.Services.AddSingleton<IClock, SystemClock>();

    var connectionString = builder.Configuration.GetConnectionString("Db");
    if (!string.IsNullOrEmpty(connectionString))
    {
        // Options must match the migrations pipeline (LifDbContextFactory): snake_case + audit stamps.
        builder.Services.AddDbContext<LifDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new AuditTimestampInterceptor()));

        // Ports → EF adapters; use cases are only callable when a database is configured.
        builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        builder.Services.AddScoped<ICurriculumReader, CurriculumReader>();
        builder.Services.AddScoped<ILearnerRepository, LearnerRepository>();
        builder.Services.AddScoped<IConceptMasteryRepository, ConceptMasteryRepository>();
        builder.Services.AddScoped<IWordStateRepository, WordStateRepository>();
        builder.Services.AddScoped<IMistakeRepository, MistakeRepository>();
        builder.Services.AddScoped<IStudySessionRepository, StudySessionRepository>();
        builder.Services.AddScoped<IReviewItemRepository, ReviewItemRepository>();
        builder.Services.AddUseCaseHandlers();
    }

    // --- AI gateway (doc 10): config-gated provider chain; no "Ai:Providers" = curated content only ---
    var aiProviderOptions = builder.Configuration.GetSection("Ai:Providers").GetChildren()
        .Select(section => section["Type"]?.ToLowerInvariant() switch
        {
            "lmstudio" => OpenAiCompatibleOptions.LmStudio(
                section["Model"] ?? throw MissingAiSetting(section.Key, "Model"),
                section["BaseUrl"] ?? "http://localhost:1234/v1/"),
            "ollama" => OpenAiCompatibleOptions.Ollama(
                section["Model"] ?? throw MissingAiSetting(section.Key, "Model"),
                section["BaseUrl"] ?? "http://localhost:11434/v1/"),
            "openai" => OpenAiCompatibleOptions.OpenAi(
                section["Model"] ?? throw MissingAiSetting(section.Key, "Model"),
                section["ApiKey"] ?? throw MissingAiSetting(section.Key, "ApiKey"),
                section["BaseUrl"] ?? "https://api.openai.com/v1/"),
            var other => throw new InvalidOperationException($"Unknown AI provider type '{other}' in Ai:Providers."),
        })
        .ToList();
    if (aiProviderOptions.Count > 0)
    {
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IReadOnlyList<IAiProvider>>(sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            return [.. aiProviderOptions.Select(o =>
                (IAiProvider)new OpenAiCompatibleProvider(httpFactory.CreateClient($"ai:{o.Name}"), o))];
        });
        builder.Services.AddSingleton<AiTaskOrchestrator>();
        builder.Services.AddSingleton<IExplainer>(sp => sp.GetRequiredService<AiTaskOrchestrator>());
        builder.Services.AddSingleton<IExerciseGenerator>(sp => sp.GetRequiredService<AiTaskOrchestrator>());
    }

    // --- Health checks: /health/live = process up; /health/ready = dependencies reachable ---
    var healthChecks = builder.Services.AddHealthChecks();
    if (!string.IsNullOrEmpty(connectionString))
    {
        healthChecks.AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);
    }

    var app = builder.Build();

    // Local mode applies migrations on startup; server mode uses migration jobs (doc 18 §2).
    if (app.Configuration["LIF_MODE"] is "local" && !string.IsNullOrEmpty(connectionString))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LifDbContext>();
        await db.Database.MigrateAsync();

        // Idempotent curriculum import so a fresh local install can onboard immediately.
        var seedPackage = app.Configuration["Seed:CurriculumPackage"]
            ?? Path.Combine(app.Environment.ContentRootPath, "seeds", "curriculum", "de");
        if (Directory.Exists(seedPackage))
        {
            await CurriculumSeedImporter.ImportAsync(db, seedPackage);
        }
    }

    app.UseSerilogRequestLogging();

    app.MapLifHealthChecks();

    // REST surface per doc 12: versioned group, problem+json failures, camelCase JSON defaults.
    app.MapGroup("/api/v1")
        .MapLearnerEndpoints()
        .MapSessionEndpoints()
        .MapReviewEndpoints();

    // SPA: built frontend assets are copied into wwwroot at image build time (doc 18 §2).
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "LIF host terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Exposed for WebApplicationFactory in Lif.Api.IntegrationTests.</summary>
public partial class Program
{
    private static InvalidOperationException MissingAiSetting(string providerKey, string setting) =>
        new($"Ai:Providers:{providerKey} is missing required setting '{setting}'.");
}
