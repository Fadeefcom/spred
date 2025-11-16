using AggregatorService.Abstractions;
using Extensions.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorService.Routes;

public static class AggregatorServiceRoutes
{
    private static void AddRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/parser", ([FromQuery] string accessKey, ITrackDownloadService trackDownloadService,
            ILogger<Program> logger, IConfiguration configuration, IParserAccessGate parserAccessGate) =>
        {
            var originalKey = configuration.GetSection("Downloader").GetValue<string>("AccessKey");
            if (parserAccessGate.IsBlocked())
                return Results.Unauthorized();
            
            if (string.IsNullOrWhiteSpace(accessKey) || !accessKey.Equals(originalKey, StringComparison.InvariantCulture))
            {
                parserAccessGate.RegisterFailure();
                return Results.Unauthorized();
            }

            var command = trackDownloadService.GetTrackFromYoutubeCommand();

            if (command != null )
                return Results.Ok(command);
            
            logger.LogSpredInformation("Parsing request", "Declined");
            return Results.BadRequest();
        }).AllowAnonymous();

        app.MapPost("/parser/{id}", async (IFormFile fromFile, Guid id, [FromQuery] string accessKey,
            [FromServices] ITrackSenderService trackSenderService, 
            [FromServices] ILogger<Program> logger, [FromServices] IConfiguration configuration, [FromServices] IParserAccessGate parserAccessGate) =>
        {
            var originalKey = configuration.GetSection("Downloader").GetValue<string>("AccessKey");
            if (parserAccessGate.IsBlocked())
                return Results.Unauthorized();
            
            if (string.IsNullOrWhiteSpace(accessKey) || !accessKey.Equals(originalKey, StringComparison.InvariantCulture))
            {
                parserAccessGate.RegisterFailure();
                return Results.Unauthorized();
            }

            await trackSenderService.PushTrack(fromFile, id);
            logger.LogSpredInformation("Parsing request", "Fetched");
            
            return Results.Created();
        }).DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(100L * 1024 * 1024),
            new RequestFormLimitsAttribute { MultipartBodyLengthLimit = 100L * 1024 * 1024 }).AllowAnonymous();

        app.MapGet("/parser/{id}/unsuccessful", (Guid id,  [FromQuery] string accessKey, [FromServices] IConfiguration configuration,
            [FromServices] ITrackSenderService trackSenderService, [FromServices] IParserAccessGate parserAccessGate) =>
        {
            var originalKey = configuration.GetSection("Downloader").GetValue<string>("AccessKey");
            if (parserAccessGate.IsBlocked())
                return Results.Unauthorized();
            
            if (string.IsNullOrWhiteSpace(accessKey) || !accessKey.Equals(originalKey, StringComparison.InvariantCulture))
            {
                parserAccessGate.RegisterFailure();
                return Results.Unauthorized();
            }

            trackSenderService.UnsuccessfulResult(id);
            return Results.Ok();

        }).AllowAnonymous();
    }

    public static void AddMapGroup(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/aggregator").AddRoutes();
    }
}