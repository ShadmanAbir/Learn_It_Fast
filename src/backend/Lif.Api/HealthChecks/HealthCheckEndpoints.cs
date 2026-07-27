using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Lif.Api.HealthChecks;

/// <summary>/health/live = process is up; /health/ready = tagged dependency checks pass (Phase 0 exit criterion).</summary>
public static class HealthCheckEndpoints
{
    public static IEndpointRouteBuilder MapLifHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
        });

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
        });

        return endpoints;
    }
}
