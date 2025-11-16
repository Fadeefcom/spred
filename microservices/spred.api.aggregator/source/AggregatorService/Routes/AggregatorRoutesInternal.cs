using Extensions.Extensions;
using MassTransit;
using Spred.Bus.Contracts;

namespace AggregatorService.Routes;

public static class AggregatorRoutesInternal
{
    private static void AddRoutesInternal(this IEndpointRouteBuilder app)
    {
        app.MapPost("/catalog/infer", async (AggregateCatalogReport request, ISendEndpointProvider sendEndpointProvider, ILogger<Program> logger) =>
        {
            logger.LogSpredInformation("CatalogInfer", $"Sending AggregateCatalogReport to exchange:catalog-infer {@request}");

            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("exchange:catalog-track-inference-request"));
            await endpoint.Send(request);

            logger.LogSpredInformation("CatalogInfer", "Inference message sent successfully");
            return Results.Accepted();
        }).AllowAnonymous();
    }
    
    public static void AddMapGroupInternal(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/iternal/aggregator").AddRoutesInternal();
    }
}