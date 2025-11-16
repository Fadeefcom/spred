using AutoMapper;
using MediatR;
using Spred.Bus.DTOs;
using StackExchange.Redis;
using TrackService.Abstractions;
using TrackService.Models.Commands;
using TrackService.Models.Entities;
using TrackService.Models.Queries;

namespace TrackService.Routes;

/// <summary>
/// Internal track routes, for services
/// </summary>
public static class InternalRoutes
{
    private static void AddInternalRoutes(this IEndpointRouteBuilder app)
    {
        // Get audio by id
        app.MapGet("/audio/{spredUserId:guid}/{id:guid}", async (Guid spredUserId, Guid id,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new GetAudioStreamCommand()
                    { TrackId = id, SpredUserId = spredUserId };

                var result = await mediator.Send(command, cancellationToken);

                if (result != null)
                    return Results.File(result, "multipart/form-data");
                return Results.NotFound("Track not found.");
            })
            .WithName("Get audio internal.")
            .Produces<Stream>()
            .WithOpenApi()
            .AllowAnonymous();

        app.MapGet("/audio/exists/{id}", async (string id,
                IBlobContainerProvider containerProvider,
                CancellationToken cancellationToken) =>
            {
                var result = await containerProvider.CheckIfExists(Guid.Parse(id), cancellationToken);

                if (result)
                    return Results.Ok();
                return Results.NotFound();
            })
            .WithName("Check exists track internal.")
            .WithOpenApi()
            .AllowAnonymous();

        app.MapPost("/{spredUserId:guid}", async (Guid spredUserId, TrackDtoWithPlatformIds trackDto, IMediator mediator) =>
            {
                var command = new CreateTrackMetadataItemCommand(trackDto, spredUserId, null);
                var result = await mediator.Send(command, CancellationToken.None);

                return Results.Ok(new { id = result });
            }).WithName("Create track internal.")
            .WithDescription("Create track without file.")
            .WithOpenApi()
            .AllowAnonymous();

        app.MapPatch("/{spredUserId:guid}/{id}",
            async (IFormFile file, Guid spredUserId, Guid id, IMediator mediator) =>
            {
                var command = new UpdateFileCommand(id, spredUserId, file);
                await mediator.Publish(command, CancellationToken.None);
                return Results.Created();
            })
            .WithName("Add audio track.")
            .WithOpenApi()
            .DisableAntiforgery()
            .AllowAnonymous();

        app.MapPatch("/{spredUserId:guid}/{id}/unsuccessful", async (Guid spredUserId, Guid id, IMediator mediator,
                IConnectionMultiplexer connectionMultiplexer) =>
            {
                var redisKey = $"track-fail:{id}";
                var db = connectionMultiplexer.GetDatabase();
                var failCount = await db.StringIncrementAsync(redisKey);
                if (failCount < 4)
                {
                    await db.KeyExpireAsync(redisKey, TimeSpan.FromDays(3));
                    return Results.Ok();
                }
                
                var command = new UpdateTrackMetadataItemCommand()
                {
                    Status = UploadStatus.Failed,
                    Id = id,
                    SpredUserId = spredUserId,
                };
                await mediator.Publish(command, CancellationToken.None);
                return Results.Created();
            }).WithName("Set failed track status.")
            .WithOpenApi()
            .DisableAntiforgery()
            .AllowAnonymous();

        app.MapGet("/{spredUserId:guid}/{id:guid}", async (Guid id, Guid spredUserId, IMediator mediator,
                IMapper mapper,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new GetTrackMetadataItemCommand(id, spredUserId), cancellationToken);

                if (result is null)
                    return Results.NotFound(new { id });

                var dto = mapper.Map<TrackDto>(result);
                return Results.Ok(dto);
            })
            .WithName("Get public track info internal.")
            .WithOpenApi()
            .Produces<TrackDto>()
            .AllowAnonymous();
    }
    
    /// <summary>
    /// Map internal, service routes
    /// </summary>
    /// <param name="app"></param>
    public static void AddMapGroupInternal(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/internal/track").AddInternalRoutes();
    }
}