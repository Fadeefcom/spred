using ActivityService.Components.Services;
using Extensions.Configuration;
using Extensions.Extensions;
using Microsoft.AspNetCore.Mvc;
using Spred.Bus.Abstractions;

namespace ActivityService.Routes;

/// <summary>
/// Provides endpoint mappings for activity-related operations,
/// including retrieving user feeds.
/// </summary>
public static class ActivityRoutes
{
    /// <summary>
    /// Maps endpoints for activity operations within the "/activities" route group.
    /// </summary>
    /// <param name="app">The route builder used to configure activity endpoints.</param>
    private static void MapActivities(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (
                [FromServices] ActivityFeedService feedService,
                [FromServices] IActorProvider actorProvider,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var queryParams = context.Request.Query
                    .ToDictionary(q => q.Key, q => q.Value.ToString());

                var offset = queryParams.GetOffset();
                var limit = queryParams.GetLimit();

                var userId = actorProvider.GetActorId();

                var feed = await feedService.GetUserFeedAsync(userId, offset, limit, cancellationToken);
                return Results.Ok(feed);
            })
            .WithName("GetUserFeed")
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);
    }

    /// <summary>
    /// Maps the activity route group under "/activities".
    /// </summary>
    /// <param name="app">The application builder used to configure endpoint groups.</param>
    public static void MapGroup(this WebApplication app)
    {
        app.MapGroup("/activities").MapActivities();
    }
}