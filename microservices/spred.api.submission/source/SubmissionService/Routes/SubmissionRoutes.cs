using Extensions.Configuration;
using Extensions.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Spred.Bus.Abstractions;
using SubmissionService.Models;
using SubmissionService.Models.Commands;
using SubmissionService.Models.Entities;
using SubmissionService.Models.Queries;

namespace SubmissionService.Routes;

/// <summary>
/// Provides endpoint mappings for submission-related operations,
/// including creation, updates, and retrieval of submissions.
/// </summary>
public static class SubmissionRoutes
{
    /// <summary>
    /// Maps endpoints for submission operations within the "/submissions" route group.
    /// </summary>
    /// <param name="app">The route builder used to configure submission endpoints.</param>
    private static void MapSubmission(this IEndpointRouteBuilder app)
    {
        app.MapPost("", async (CreateSubmissionRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new CreateSubmissionCommand(request), cancellationToken);
            return Results.Created($"/submissions/{request.CatalogItemId}/{result.SubmissionId}", new { result });
        }).WithName("CreateSubmission").RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);

        app.MapPatch("/{catalogId:guid}/{id:guid}/status", async (Guid catalogId, Guid id, UpdateSubmissionStatusRequest req, [FromServices] IMediator mediator, CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse(req.NewStatus, out SubmissionStatus status)) return Results.BadRequest($"Invalid status: {req.NewStatus}");
            await mediator.Send(new UpdateSubmissionStatusCommand(id, req.ArtistId, catalogId, status), cancellationToken);
            return Results.NoContent();
        }).WithName("UpdateSubmission").RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);

        app.MapGet("/{catalogId:guid}", async (Guid catalogId, [FromQuery] string? status, [FromServices] IMediator mediator, HttpContext context, CancellationToken cancellationToken) =>
        {
            var queryParams = context.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            SubmissionStatus? submissionStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<SubmissionStatus>(status, true, out var parsed)) return Results.BadRequest($"Invalid status: {status}");
                    submissionStatus = parsed;
            }
            var items = await mediator.Send(new GetSubmissionsByCatalogQuery(catalogId, submissionStatus, queryParams.GetOffset(), queryParams.GetLimit()), cancellationToken);
            return Results.Ok(items);
        }).WithName("GetSubmissions").RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);

        app.MapGet("/{catalogId:guid}/{id:guid}", async (Guid catalogId, Guid id, [FromServices] IMediator mediator, CancellationToken cancellationToken) =>
        {
            var item = await mediator.Send(new GetSubmissionByIdQuery(catalogId, id), cancellationToken);
            return Results.Ok(item);
        }).WithName("GetSubmission").RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);

        app.MapGet("", async ([FromQuery] string? status, [FromServices] IMediator mediator,  HttpContext context, CancellationToken cancellationToken) =>
        {
            var queryParams = context.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            SubmissionStatus? submissionStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<SubmissionStatus>(status, true, out var parsed)) return Results.BadRequest($"Invalid status: {status}");
                submissionStatus = parsed;
            }
            var items = await mediator.Send(new GetMySubmissionsQuery(submissionStatus, queryParams.GetOffset(), queryParams.GetLimit()), cancellationToken);
            return Results.Ok(items);
        }).WithName("GetArtistsSubmissions").RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);
        
        app.MapGet("/stats", async (IActorProvider actorProvider, IMediator mediator) =>
        {
            var query = new GetSubmissionStats(actorProvider.GetActorId());
           var result = await mediator.Send(query);
           return Results.Ok(result);
        }).WithName("GetStats").RequireAuthorization(JwtSpredPolicy.JwtUserPolicy);
    }

    /// <summary>
    /// Maps the submission route group under "/submissions".
    /// </summary>
    /// <param name="app">The application builder used to configure endpoint groups.</param>
    public static void MapGroup(this WebApplication app)
    {
        app.MapGroup("/submissions").MapSubmission();
    }
}
