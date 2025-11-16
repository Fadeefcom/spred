using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InferenceService.Routes;

/// <summary>
/// Define Health routes
/// </summary>
public static class HealtzRoutes
{
    /// <summary>
    /// AddAsync routes to http pipeline
    /// </summary>
    /// <param name="app"></param>
    public static void AddHealthRoutes(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/healtz", new HealthCheckOptions()
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            Predicate = _ => true
        });

        app.MapHealthChecks("/healtz/readiness", new HealthCheckOptions()
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            Predicate = _ => true
        });

        app.MapHealthChecks("/healtz/liveness", new HealthCheckOptions()
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            Predicate = _ => true
        });
    }
}