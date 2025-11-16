using InferenceService.Models.Dto;
using MassTransit;

namespace InferenceService.Routes;

/// <summary>
/// Provides routes for internal inference operations.
/// </summary>
public static class InternalRoutes
{
    private static void AddRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("", async (TrackEmbeddingResult result, IPublishEndpoint publishEndpoint, CancellationToken cancellationToken) =>
        {
            await publishEndpoint.Publish(result, cancellationToken);
            return Results.Accepted();
        });
    }
    
    /// <summary>
    /// Adds the inference route group to the endpoint route builder.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder with the added route group.</returns>
    public static IEndpointRouteBuilder AddMapInternalGroup(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/internal/inference").AddRoutes();
        return app;
    }
}