using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AutoMapper;
using Extensions.Configuration;
using Extensions.Middleware;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlaylistService.Models.Commands;
using PlaylistService.Models.DTO;
using PlaylistService.Models.Entities;
using PlaylistService.Models.Queries;
using Spred.Bus.DTOs;

namespace PlaylistService.Routes;

/// <summary>
/// Provides route mappings for playlist-related operations.
/// </summary>
public static class PlaylistRoutes
{
    /// <summary>
    /// Adds playlist-related routes to the endpoint route builder.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    private static void AddRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("", async (IMediator mediator,
                HttpContext context,
                IMapper mapper,
                CancellationToken cancellationToken) =>
            {
                var queryParams = context.Request.Query
                    .ToDictionary(q => q.Key, q => q.Value.ToString());

                var authorId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
                var type = ResolveType(queryParams.TryGetValue("type", out var value) 
                           && !string.IsNullOrEmpty(value)
                            ? value
                            : "playlist");
                if (string.IsNullOrWhiteSpace(type))
                    return Results.BadRequest("Invalid type.");
                
                var result = await mediator.Send(new GetCatalogMetadataQuery() { Type = type, 
                        SpredUserId = Guid.Parse(authorId), Query = queryParams },
                    cancellationToken);

                var playlistDto = mapper.Map<List<PublicMetadataDto>>(result);
                return Results.Ok(playlistDto);
            })
            .WithName("Get metadata")
            .WithDescription("Gets available metadata for current user")
            .Produces<List<PublicMetadataDto>>()
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.PlaylistOwnPrivateRead);

        app.MapPost("", async (
                [FromBody] PublicMetadataDto playlistDto,
                IMediator mediator,
                HttpContext context,
                IMapper mapper,
                CancellationToken cancellationToken) =>
            {
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(playlistDto);

                if (!Validator.TryValidateObject(playlistDto, validationContext, validationResults, true))
                    return Results.BadRequest(validationResults.Select(v => v.ErrorMessage));
                
                var type = ResolveType(!string.IsNullOrWhiteSpace(playlistDto.Type)
                    ? playlistDto.Type
                    : "playlist");
                if(string.IsNullOrWhiteSpace(type))
                    return Results.BadRequest("Invalid type.");
                
                var authorId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
                var command = mapper.Map<CreateMetadataCommand>(playlistDto);
                command.SpredUserId = Guid.Parse(authorId);
                command.Type = type;
                
                var result = await mediator.Send(command,
                    cancellationToken);

                return Results.Ok(new { id = result});
            })
            .WithName("Add public metadata")
            .WithDescription("Add metadata to current user library")
            .Produces(204)
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.PlaylistCreate);

        app.MapPatch("/{id:guid}", async (Guid id,
                PublicMetadataDto playlistDto,
                IMediator mediator,
                HttpContext context,
                IMapper mapper,
                CancellationToken cancellationToken) =>
            {                
                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(playlistDto);

                if (!Validator.TryValidateObject(playlistDto, validationContext, validationResults, true))
                    return Results.BadRequest(validationResults.Select(v => v.ErrorMessage));

                var authorId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
                var command = new UpdateMetadataCommand(playlistDto)
                {
                    Id = id,
                    SpredUserId = Guid.Parse(authorId)
                };
                await mediator.Publish(command,
                    cancellationToken);

                return Results.NoContent();
            })
            .WithName("UpdateAsync metadata")
            .WithDescription("UpdateAsync user metadata")
            .Produces(204)
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.PlaylistEditOwn);
        
        app.MapDelete("/{id:guid}", async (Guid id,
                IMediator mediator,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var authorId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
                var result = await mediator.Send(
                    new DeleteMetadataCommand() { SpredUserId = Guid.Parse(authorId), PlaylistId = id },
                    cancellationToken);
                
                return result ? Results.NoContent() : Results.NotFound();
            })
            .WithName("DeleteAsync metadata")
            .WithDescription("DeleteAsync user metadata")
            .Produces(204)
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.PlaylistDeleteOwn);

        app.MapGet("/{id:guid}", async (Guid id,
                IMediator mediator, IMapper mapper, HttpContext context, CancellationToken cancellationToken) =>
            {
                var authorId = context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
                
                var result = await mediator.Send(new GetMetadataByIdQuery() {
                        PlaylistId = id, 
                        SpredUserId = Guid.Parse(authorId),
                        IncludeStatistics = false
                    },
                    cancellationToken);
                var dto = mapper.Map<PublicMetadataDto>(result.Item1);
                return dto == null ? Results.NotFound() : Results.Ok(dto);
            })
            .WithName("Get specific metadata")
            .WithDescription("Get metadata by id")
            .Produces<MetadataDto>()
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.PlaylistOwnPrivateRead);
        
        app.MapGet("/{spredUserId:guid}/{id:guid}", async (Guid id, Guid spredUserId,
                IMediator mediator, IMapper mapper, CancellationToken cancellationToken) =>
            {
                (CatalogMetadata? catalogMetadata, var followerChange) = await mediator.Send(new GetMetadataByIdQuery() {PlaylistId = id, 
                        SpredUserId = spredUserId,
                        IncludeStatistics = true },
                    cancellationToken);
                PublicMetadataDto dto = mapper.Map<PublicMetadataDto>(catalogMetadata);
                if(dto != null)
                    dto.FollowerChange = followerChange;

                if (dto is { IsPublic: true })
                    return Results.Ok(dto);
                
                if (dto is { IsPublic: false })
                    return Results.NoContent();

                return Results.NotFound(new { id });
            })
            .AddEndpointFilter<CacheFilterEndpoint<PublicMetadataDto>>()
            .WithName("Get public metadata")
            .WithDescription("Get metadata by id")
            .Produces<PublicMetadataDto>()
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.PlaylistPublicRead);
    }

    /// <summary>
    /// Adds the playlist route group to the endpoint route builder.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void AddMapGroup(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/playlist").AddRoutes();
    }

    /// <summary>
    /// Resolve type for metadata
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static string  ResolveType(string type)
    {
        var resolvedType = type.ToLowerInvariant() switch
        {
            "playlist" => "playlist",
            "playlistmetadata" => "playlist",
            "record" => "record",
            "record_label" => "record",
            "recordlabel" => "record",
            "recordlabelmetadata" => "record",
            "radio" => "radio",
            "radio_station" => "radio",
            "radiometadata" => "radio",
            _ => string.Empty
        };

        return resolvedType;
    }
}
