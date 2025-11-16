using System.Collections.Concurrent;
using System.Globalization;
using AggregatorService.Abstractions;
using AggregatorService.Models.Commands;
using Extensions.Extensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Repository.Abstractions.Models;

namespace AggregatorService.Components;

/// <inheritdoc cref="ITrackDownloadService"/>
public class TrackDownloadService : ITrackDownloadService, IDisposable
{
    private readonly ConcurrentStack<FetchTrackCommand> _tracksToAdd = new();
    
    private readonly Container _trackContainer;
    private readonly SemaphoreSlim _populateLock = new(1, 1);
    private readonly ILogger<TrackDownloadService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackDownloadService"/> class.
    /// Sets up Cosmos DB client, track container, and starts background preload of track metadata.
    /// </summary>
    /// <param name="loggerFactory">Factory used to create a logger instance.</param>
    /// <param name="dbConnectionOptions">Database connection options including endpoint, key, and DB name.</param>
    /// <param name="cosmosClient">Cosmos client.</param>
    public TrackDownloadService(ILoggerFactory loggerFactory, IOptions<DbConnectionOptions> dbConnectionOptions, CosmosClient cosmosClient)
    {
        var dbName = dbConnectionOptions.Value.DatabaseName;
        
        _trackContainer = cosmosClient.GetContainer(dbName, "TrackMetadata_v2");
        _logger = loggerFactory.CreateLogger<TrackDownloadService>();
        _ = PopulateTracks();
    }

    /// <inheritdoc cref="ITrackDownloadService"/>
    public FetchTrackCommand? GetTrackFromYoutubeCommand()
    {
        _tracksToAdd.TryPop(out var result);

        if (_tracksToAdd.Count < 100)
            _ = PopulateTracks();
        
        return result;
    }

    private async Task PopulateTracks()
    {
        if (!await _populateLock.WaitAsync(0))
            return;
        
        var trackInProcess = new HashSet<string>(
            _tracksToAdd.Select(x => x.Id.ToString())
        );

        try
        {
            for (int i = 0; i < 10; i++)
            {
                var query = new QueryDefinition(@"
                    SELECT * FROM c 
                    WHERE c.Status = 0 ORDER BY c._ts DESC");
                
                var partitionKey = new PartitionKeyBuilder()
                    .Add(Guid.Empty.ToString())
                    .Add(i.ToString("D2", CultureInfo.InvariantCulture))
                    .Build();

                using var iterator = _trackContainer.GetItemQueryIterator<JObject>(
                    queryDefinition: query,
                    requestOptions: new QueryRequestOptions
                    {
                        PartitionKey = partitionKey
                    }
                );

                int trackCount = 0;

                while (iterator.HasMoreResults && trackCount < 1000)
                {
                    var response = await iterator.ReadNextAsync();
                    foreach (var item in response)
                    {
                        var title = item["Title"]?.ToString();
                        var artists = item["Artists"] as JArray;
                        var firstArtistName = artists?.FirstOrDefault()?["Name"]?.ToString();

                        var prompt = $"{title} {firstArtistName}".Trim();

                        var command = new FetchTrackCommand()
                        {
                            Id = Guid.Parse(item["id"]!.ToString()),
                            PrimaryId = item["PrimaryId"]!.ToString(),
                            Prompt = prompt
                        };

                        if (!trackInProcess.Contains(command.Id.ToString()))
                        {
                            trackCount++;
                            _tracksToAdd.Push(command);
                            trackInProcess.Add(command.Id.ToString());
                        }

                        if (trackCount > 1000)
                            break;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogSpredError("TrackDownloadService:PopulateTracks", ex);
        }
        finally
        {
            _populateLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _populateLock.Dispose();
        GC.SuppressFinalize(this);
    }
}