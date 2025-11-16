using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AutoMapper;
using Extensions.Configuration;
using Extensions.Extensions;
using Extensions.Middleware;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Spred.Bus.DTOs;
using StackExchange.Redis;
using TrackService.Middleware;
using TrackService.Models;
using TrackService.Models.Commands;
using TrackService.Models.DTOs;
using TrackService.Models.Queries;

namespace TrackService.Routes;

/// <summary>
/// Static class containing route definitions for TrackService.
/// </summary>
public static class TrackServiceRoutes
{

    /// <summary>
    /// Adds routes to the HTTP pipeline.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    private static void AddRoutes(this IEndpointRouteBuilder app)
    {
        // Add track
        app.MapPost("/",
            async (IFormFile file, HttpContext context, IMediator mediator, CancellationToken cancellationToken,
                IMapper mapper) =>
            {
                var trackCreate = context.Items.First(i => (string)i.Key == "Track").Value as TrackCreate;
                var trackDto = mapper.Map<TrackDto>(trackCreate);
                trackDto.Artists.Add(new ArtistDto()
                {
                    Name = context.User.Claims.First(c => c.Type == ClaimTypes.Name).Value,
                    PrimaryId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value
                });

                var spredUserId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
                var command = new CreateTrackMetadataItemCommand(trackDto, Guid.Parse(spredUserId), file);

                var result = await mediator.Send(command, cancellationToken);

                return Results.Ok(new { id = result });
            })
            .AddEndpointFilter<TrackCreateParser>()
            .AddEndpointFilter<UploadRateLimiter>()
            .WithName("Add track.")
            .WithDescription("Add track to repository")
            .WithOpenApi()
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(100L * 1024 * 1024),
                new RequestFormLimitsAttribute { MultipartBodyLengthLimit = 100L * 1024 * 1024 })
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.TrackCreate);

        // Delete track by id
        app.MapDelete("/{id:guid}",
            async (Guid id, IMediator mediator, HttpContext context, CancellationToken cancellationToken) =>
            {
                var spredUserId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
                var deleteCommand = new DeleteTrackMetadataItemCommand(id, Guid.Parse(spredUserId));
                await mediator.Publish(deleteCommand, cancellationToken);

                return Results.NoContent();
            })
            .WithName("Delete track.")
            .WithOpenApi()
            .Produces(204)
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.TrackDeleteOwn);

        // Get track by id
        app.MapGet("/{id:guid}", async (Guid id, HttpContext context, IMediator mediator, IMapper mapper,
                CancellationToken cancellationToken) =>
            {
                var spredUserId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
                var result = await mediator.Send(
                    new GetTrackMetadataItemCommand(id, Guid.Parse(spredUserId)), cancellationToken);

                if (result is null)
                    return Results.NotFound();

                var dto = mapper.Map<PrivateTrackDto>(result);

                return Results.Ok(dto);
            })
            .WithName("Get track.")
            .WithDescription("Get specific track.")
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.TrackOwnPrivateRead);

        // Get tracks by query
        app.MapGet("",
            async (HttpContext context, IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var queryParams = context.Request.Query
                    .ToDictionary(q => q.Key, q => q.Value.ToString());

                var spredUserId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
                var command = new GetTrackMetadataByQueryCommand()
                    { QueryParams = queryParams, SpredUserId = Guid.Parse(spredUserId) };

                var result = await mediator.Send(command, cancellationToken);

                return Results.Ok(result);
            })
            .WithName("Get track metadata.")
            .WithDescription("Get tracks by query model.")
            .WithOpenApi()
            .Produces<TracksResponseModel>()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.TrackOwnPrivateRead);

        // Update track
        app.MapPatch("/{id:guid}", async (Guid id, [FromBody] PrivateTrackDto dto, HttpContext context,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(dto);

                if (!Validator.TryValidateObject(dto, validationContext, validationResults, true))
                    return Results.BadRequest(validationResults.Select(v => v.ErrorMessage));

                //hasherHelper.ValidateHash(dto, HashAlgorithmType.Md5);

                var spredUserId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
                var command = new UpdateTrackMetadataItemCommand(dto, id, Guid.Parse(spredUserId));
                await mediator.Publish(command, cancellationToken);

                return Results.NoContent();
            })
            .WithName("Update track.")
            .WithDescription("Updates specific track")
            .WithOpenApi()
            .Produces(204)
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.TrackEditOwn);

        // Get audio by id
        app.MapGet("/audio/{id}", async (string id,
                IMediator mediator,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var spredUserId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
                var command = new GetAudioStreamCommand()
                    { TrackId = Guid.Parse(id), SpredUserId = Guid.Parse(spredUserId) };

                var result = await mediator.Send(command, cancellationToken);

                if (result != null)
                    return Results.File(result, "multipart/form-data");
                return Results.NotFound("Track not found.");
            })
            .WithName("Get audio.")
            .Produces<Stream>()
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.TrackOwnPrivateRead);

        app.MapGet("/{platform}/{spredUserId:guid}/{id:guid}", async (Guid id, Guid spredUserId, string platform, IMediator mediator,
                IMapper mapper,
                CancellationToken cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(platform))
                    platform = "spotify";
                else
                    platform = platform.ToLowerInvariant();
                
                var result = await mediator.Send(
                    new GetTrackMetadataItemCommand(id, spredUserId), cancellationToken);

                if (result is null)
                    return Results.NotFound();

                var dto = mapper.Map<PublicTrackDto>(result);
                dto.TrackUrl = result.TrackLinks.FirstOrDefault(t => t.Platform == platform)?.Value ?? string.Empty;
                return Results.Ok(dto);
            })
            .AddEndpointFilter<CacheFilterEndpoint<PublicTrackDto>>()
            .WithName("Get public track info.")
            .WithOpenApi()
            .Produces<PublicTrackDto>()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.TrackPublicRead);

        app.MapGet("/upload/limit", async (HttpContext context, IConnectionMultiplexer redis) =>
            {
                var user = context.User;
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Results.Unauthorized();
                
                if (user.IsPremium())
                {
                    var premiumResult = new
                    {
                        limit = (string?)"unlimited",
                        used = 0,
                        remaining = (string?)"unlimited",
                        reset = 0L
                    };
                    return Results.Ok(premiumResult);
                }

                var db = redis.GetDatabase();
                var key = $"track:limit:{userId}";
                var count = await db.StringGetAsync(key);
                var ttl = await db.KeyTimeToLiveAsync(key);

                var used = (int)(count.HasValue ? (long)count : 0);
                var remaining = Math.Max(0, 3 - used);
                var resetTime = DateTimeOffset.UtcNow.Add(ttl ?? TimeSpan.Zero);

                var result = new
                {
                    limit = UploadRateLimiter.FreeTierTrackLimit,
                    used,
                    remaining,
                    reset = resetTime
                };

                return Results.Ok(result);
            })
            .WithName("Get limit upload.")
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);
    }

    /// <summary>
    /// Adds the map group for track routes.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder with the added map group.</returns>
    public static IEndpointRouteBuilder AddMapGroup(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/track").AddRoutes();
        return app;
    }
}