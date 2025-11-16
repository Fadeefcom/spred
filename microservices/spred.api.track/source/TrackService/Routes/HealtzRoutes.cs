using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TrackService.Routes;

/// <summary>
/// Provides extension methods to define health check routes for the application.
/// </summary>
public static class HealtzRoutes
{
    /// <summary>
    /// Adds health check routes to the HTTP request pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IEndpointRouteBuilder"/> to add the routes to.</param>
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
