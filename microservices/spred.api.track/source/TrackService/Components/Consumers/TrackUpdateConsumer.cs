using Extensions.Extensions;
using Extensions.Utilities;
using MassTransit;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Interfaces;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;
using TrackService.Models.Commands;
using TrackService.Models.Entities;

namespace TrackService.Components.Consumers;

/// <summary>
/// Track update consumer
/// </summary>
public class TrackUpdateConsumer : IConsumer<TrackUpdateRequest>
{
    private readonly IPersistenceStore<TrackMetadata, Guid> _trackStore;
    private readonly ILogger<TrackUpdateConsumer> _logger;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="trackStore"></param>
    /// <param name="loggerFactory"></param>
    public TrackUpdateConsumer(IPersistenceStore<TrackMetadata, Guid> trackStore, ILoggerFactory loggerFactory)
    {
        _trackStore = trackStore;
        _logger = loggerFactory.CreateLogger<TrackUpdateConsumer>();
    }
    
    /// <summary>
    /// Consume handler
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<TrackUpdateRequest> context)
    {
        _logger.LogSpredInformation("Track Aggregation Consume",$"Consuming {context.Message.TrackId}.");
        var newGenre = context.Message.Genre;

        if (!string.IsNullOrWhiteSpace(newGenre))
        {
            var command = new UpdateTrackMetadataItemCommand()
            {
                Id = context.Message.TrackId,
                SpredUserId = context.Message.SpredUserId,
                Audio  = new AudioFeaturesDto()
                {
                    Genre = newGenre
                },
            };

            var result = await _trackStore.GetAsync(context.Message.TrackId,
                GetPartitionKey(context.Message.TrackId, context.Message.SpredUserId), CancellationToken.None);

            result.Result!.Update(command);
            await _trackStore.UpdateAsync(result.Result, CancellationToken.None);
        }

        _logger.LogSpredInformation("Track Aggregation finished",$"Updated {context.Message.TrackId}. with {newGenre}");
    }

    private static PartitionKey GetPartitionKey(Guid trackId, Guid spredUserId)
    {
        var builder = new PartitionKeyBuilder()
            .Add(spredUserId.ToString());

        if (spredUserId == Guid.Empty)
        {
            builder.Add(GuidShortener.GenerateBucketFromGuid(trackId));
        }
        else
            builder.Add("00");
        
        return  builder.Build();
    }
}