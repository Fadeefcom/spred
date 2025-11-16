using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PlaylistService.Models.Commands;
using PlaylistService.Models.Queries;
using Spred.Bus.DTOs;

namespace PlaylistService.Routes;

/// <summary>
/// Internal playlist routes, for services
/// </summary>
public static class InternalRoutes
{
    private static void AddInternalRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/{authorId:guid}", async (
                Guid authorId,
                [FromBody] MetadataDto playlistDto,
                IMediator mediator,
                IMapper mapper) =>
            {
                playlistDto.SpredUserId = authorId;

                var type = PlaylistRoutes.ResolveType(!string.IsNullOrWhiteSpace(playlistDto.Type)
                    ? playlistDto.Type
                    : "playlist");
                if (string.IsNullOrWhiteSpace(type))
                    return Results.BadRequest("Invalid type");

                var command = mapper.Map<CreateMetadataCommand>(playlistDto);
                command.Type = type;

                var result = await mediator.Send(command,
                    CancellationToken.None);

                return Results.Ok(new { id = result });
            })
            .WithName("Add Catalog internal.")
            .WithDescription("Add metadata to current user library")
            .Produces<Guid>()
            .WithOpenApi();

        app.MapGet("/{authorId:guid}/{id:guid}", async (Guid id, Guid authorId,
                IMediator mediator, IMapper mapper, CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(new GetMetadataByIdQuery()
                    {
                        PlaylistId = id,
                        SpredUserId = authorId,
                        IncludeStatistics = false
                    },
                    cancellationToken);
                
                if(result.Item1 is null)
                    return Results.NotFound(new { id });
                
                var dto = mapper.Map<MetadataDto>(result.Item1);
                return Results.Ok(dto);
            })
            .WithName("Get Catalog by id internal.")
            .WithDescription("Get metadata by id")
            .Produces<MetadataDto>()
            .WithOpenApi();
    }

    /// <summary>
    /// Map internal, service routes
    /// </summary>
    /// <param name="app"></param>
    public static void AddMapGroupInternal(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/internal/playlist").AddInternalRoutes();
    }
}