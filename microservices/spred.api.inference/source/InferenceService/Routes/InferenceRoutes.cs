using System.Security.Claims;
using System.Text.RegularExpressions;
using Extensions.Configuration;
using Extensions.Extensions;
using InferenceService.Abstractions;
using InferenceService.Configuration;
using InferenceService.Helpers;
using InferenceService.Models.Dto;
using InferenceService.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace InferenceService.Routes;

/// <summary>
/// Provides routes for inference operations.
/// </summary>
public static class InferenceRoutes
{
    /// <summary>s
    /// Adds the inference routes to the endpoint route builder.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    private static void AddRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/{id:guid}", async (Guid id, ushort? limit, ushort? offset, string? type, IOptions<ModelVersion> modelVersion, IInferenceManager inferenceRepository,
                HttpContext context, CancellationToken cancellationToken) =>
            {
                if(limit > 10)
                    return Results.BadRequest("Invalid limit.");
                
                if(!string.IsNullOrEmpty(type) && !CatalogTypeHelper.IsValid(type))
                    return Results.BadRequest("Invalid type.");
                
                var spredUserId = Guid.Parse(context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

                var previousResults = await inferenceRepository.GetInference(id, spredUserId, context.User.IsPremium(), modelVersion.Value.Version, cancellationToken);

                if (previousResults.Item3 != null && previousResults.Item3.Count != 0)
                {
                    List<InferenceMetadataDto> metadata = !string.IsNullOrWhiteSpace(type)
                        ? previousResults.Item3.Where(m => m.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                            .Skip(offset ?? 0).Take(limit ?? 10).ToList()
                        : previousResults.Item3.Skip(offset ?? 0).Take(limit ?? 10).ToList();

                    return Results.Ok(new InferenceResultDto
                    {
                        Id = previousResults.Item2,
                        ModelInfo = previousResults.Item1,
                        Metadata = metadata
                    });
                }
                
                return Results.NotFound();
            })
            .WithName("Get Inference by GUID")
            .WithDescription(
                "Processes an audio file to generate Inferences using ONNX models. The audio file is fetched from an external Track API, analyzed, and the Inferences are stored in the repository.")
            .WithOpenApi()
            .Produces<InferenceResultDto>()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.TrackOwnPrivateRead);
        
        app.MapPatch("/rate/{id:guid}/{playlistId:guid}", async ([FromBody] UpdateRateRequest rateRequest,
                Guid playlistId, Guid id,
                HttpContext context,
                IInferenceManager inferenceRepository,
                CancellationToken cancellationToken) =>
            {
                var modelVersionRegex = new Regex(@"^v\d+\.\d+\.\d+$");
                if (string.IsNullOrWhiteSpace(rateRequest.ModelVersion) ||
                    !modelVersionRegex.IsMatch(rateRequest.ModelVersion))
                {
                    return Results.BadRequest("Invalid model version or status");
                }

                var spredUserId =
                    Guid.Parse(context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

                await inferenceRepository.AddRateToPlaylist(playlistId, id, spredUserId,
                    rateRequest.ModelVersion, new ReactionStatus(){ IsLiked = rateRequest.IsLiked, HasApplied = rateRequest.HasApplied, WasAccepted = rateRequest.WasAccepted}, cancellationToken);

                return Results.NoContent();
            })
            .WithName("Add Rate")
            .WithDescription("Apply rate to playlist inference result.")
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.TrackEditOwn);

        app.MapGet("/status/{id:guid}", async (Guid id, HttpContext context, IConnectionMultiplexer connection) =>
            {
                var spredUserId = Guid.Parse(context.User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
                var db = connection.GetDatabase();

                var cacheKey = $"inference:{id}:{spredUserId}";
                var trackStatus = await db.StringGetAsync(cacheKey);

                return !trackStatus.HasValue ? Results.NotFound() : Results.Ok(new { Status = (string)trackStatus! });
            })
            .WithName("Get Status")
            .WithOpenApi()
            .RequireAuthorization(JwtSpredPolicy.JwtUserPolicy, Policies.TrackOwnPrivateRead);
    }

    /// <summary>
    /// Adds the inference route group to the endpoint route builder.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The endpoint route builder with the added route group.</returns>
    public static IEndpointRouteBuilder AddMapGroup(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/inference").AddRoutes();
        return app;
    }
}