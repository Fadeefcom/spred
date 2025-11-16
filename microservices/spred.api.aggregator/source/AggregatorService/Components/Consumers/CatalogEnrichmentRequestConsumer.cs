using AggregatorService.Abstractions;
using Extensions.Extensions;
using MassTransit;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;

namespace AggregatorService.Components.Consumers;

/// <summary>
/// Consumer that handles <see cref="CatalogEnrichmentRequest"/> messages by fetching metadata,
/// statistics, and tracks from a catalog provider (e.g., Soundcharts) and publishing an update.
/// </summary>
public class CatalogEnrichmentRequestConsumer : IConsumer<CatalogEnrichmentRequest>
{
    private readonly ICatalogProvider _catalogProvider;
    private readonly ILogger<CatalogEnrichmentRequestConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpointProvider;

    /// <summary>
    /// Represents a consumer for processing catalog enrichment requests using Chartmetrics data.
    /// </summary>
    public CatalogEnrichmentRequestConsumer(
        ICatalogProvider catalogProvider,
        ILoggerFactory loggerFactory,
        IPublishEndpoint publishEndpointProvider)
    {
        _catalogProvider = catalogProvider;
        _logger = loggerFactory.CreateLogger<CatalogEnrichmentRequestConsumer>();
        _publishEndpointProvider = publishEndpointProvider;
    }

    /// <summary>
    /// Handles the consumption of a catalog enrichment request message.
    /// </summary>
    /// <param name="context">The context containing the catalog enrichment request message.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task Consume(ConsumeContext<CatalogEnrichmentRequest> context)
    {
        var request = context.Message;

        try
        {
            CatalogEnrichmentUpdateOrCreate? update = null;

            if (request.Type == "playlistMetadata")
            {
                update = await HandlePlaylistAsync(request);
            }
            else if (request.Type == "radioMetadata")
            {
                update = await HandleRadioAsync(request);
            }

            if (update != null)
            {
                update.Snapshot.SpredUserId = request.SpredUserId;
                update.Snapshot.PrimaryId = request.PrimaryId;
                update.Snapshot.Id = request.Id;
                update.Snapshot.Type = request.Type;
                
                await _publishEndpointProvider.Publish(update);
                _logger.LogSpredInformation("CatalogEnrichmentCompleted",
                    $"Request for id:{request.Id} completed successfully. Platform={request.Platform}");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogSpredError("CatalogEnrichmentFailed", $"Request for id:{request.Id} failed", ex);
            throw;
        }
    }
    
    private async Task<CatalogEnrichmentUpdateOrCreate?> HandlePlaylistAsync(CatalogEnrichmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SoundChartsApi))
        {
            var playlistId = await _catalogProvider.ResolvePlaylistIdAsync(
                request.PrimaryId.Split(':').ElementAtOrDefault(2) ?? string.Empty, request.Platform.ToLowerInvariant());

            if (string.IsNullOrWhiteSpace(playlistId))
            {
                _logger.LogSpredWarning("CatalogEnrichment",
                    $"Could not resolve playlistId for {request.PrimaryId}");
                return null;
            }

            request.SoundChartsApi = playlistId;
        }

        var metadata = await _catalogProvider.GetPlaylistMetadataAsync(
            request.SoundChartsApi, request.Platform.ToLowerInvariant());
        if (metadata == null)
        {
            _logger.LogSpredWarning("CatalogEnrichment",
                $"Metadata not found for {request.SoundChartsApi}");
            return null;
        }

        metadata.Id = request.Id;
        metadata.PrimaryId = request.PrimaryId;

        var stats = await _catalogProvider.GetPlaylistStatsAsync(
            request.SoundChartsApi, request.Platform.ToLowerInvariant(), request.UpdateStatsInfo);

        var tracks = await _catalogProvider.GetPlaylistTracksSnapshotAsync(
            request.SoundChartsApi, request.Platform.ToLowerInvariant(), DateTime.UtcNow);

        return new CatalogEnrichmentUpdateOrCreate
        {
            Snapshot = metadata,
            Stats = stats,
            Tracks = tracks
        };
    }
    
    private async Task<CatalogEnrichmentUpdateOrCreate?> HandleRadioAsync(CatalogEnrichmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SoundChartsApi))
        {
            _logger.LogSpredWarning("RadioIdEmpty",
                $"Could not resolve radioId for {request.SoundChartsApi}, {request.Id}, {request.PrimaryId}");
            return null;
        }

        var radioInfo = await _catalogProvider.GetRadioMetadataAsync(
            request.SoundChartsApi);
        if (radioInfo == null)
        {
            _logger.LogSpredWarning("CatalogEnrichment",
                $"Radio metadata not found for {request.SoundChartsApi}");
            return null;
        }

        var tracks = await _catalogProvider.GetRadioTracksSnapshotAsync(
            request.SoundChartsApi, 100);
        
        var metadata = new MetadataDto()
        {
            PrimaryId = request.PrimaryId,
            Id = request.Id,
            SoundChartsId = request.SoundChartsApi,
            Name = radioInfo.Name,
            TracksTotal = (uint)tracks.total,
            Reach = radioInfo.Reach,
            Country = radioInfo.CountryName,
            CountryCode = radioInfo.CountryCode,
            TimeZone = radioInfo.TimeZone,
            ImageUrl = radioInfo.ImageUrl,
            CityName = radioInfo.CityName
        };

        var platforms = await _catalogProvider.GetRadioPlatforms(request.SoundChartsApi);

        foreach (var platform in platforms)
        {
            metadata.ListenUrls.TryAdd(platform.Platform, platform.Url);
        }

        return new CatalogEnrichmentUpdateOrCreate
        {
            Snapshot = metadata,
            Stats = [new StatInfo()
            {
                Value = (uint)radioInfo.Reach,
                Timestamp = DateTime.Now
            }],
            Tracks = tracks.Item1
        };
    }
}