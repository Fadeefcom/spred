using System.Net;
using AggregatorService.Abstractions;
using AggregatorService.Models;
using Extensions.Extensions;
using Extensions.Utilities;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace AggregatorService.Components;

/// <inheritdoc />
public class CatalogService : ICatalogService
{
    private readonly Container _catalogContainer;
    private readonly Container _trackContainer;
    private readonly Container _enrichmentContainer;
    private readonly ILogger<CatalogService> _logger;
    private readonly TimeSpan[] _delays = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8)];

    /// <summary>
    /// Catalog service constructor.
    /// </summary>
    public CatalogService(IWebHostEnvironment env, CosmosClient cosmosClient, IConfiguration configuration, ILogger<CatalogService> logger)
    {
        var databaseName = configuration["DbConnectionOptions:DatabaseName"] ??
                           throw new ArgumentException("Database name not configured.");

        _catalogContainer = cosmosClient.GetContainer(databaseName, "CatalogMetadata_v2");
        _trackContainer = cosmosClient.GetContainer(databaseName, "TrackMetadata_v2");
        _enrichmentContainer = cosmosClient.GetContainer(databaseName, "CatalogEnrichment_v1");
        _logger = logger;
        
        if(env.EnvironmentName == "Test")
            _delays = [TimeSpan.FromSeconds(0)];
    }

    /// <inheritdoc />
    public async Task CatalogAggregateReport(int bucket, Guid id, string type, string shortDate)
    {
        try
        {
            var updated = await EnrichCatalogAsync(bucket, id);
            updated.Status = 1;

            var chunkSize = 100;

            var chunks = updated.catalogInferenceResponces
                .Select((item, index) => new { item, index })
                .GroupBy(x => x.index / chunkSize)
                .Select(g => g.Select(x => x.item).ToList())
                .ToList();

            int partIndex = 0;
            foreach (var chunk in chunks)
            {
                var partial = new CatalogInference
                {
                    id = Guid.NewGuid(),
                    Type = updated.Type,
                    Bucket = updated.Bucket,
                    catalogInferenceResponces = chunk,
                    EnrichedAt = updated.EnrichedAt,
                    EnrichedDate = shortDate,
                    Status = updated.Status
                };

                var partitionKey = new PartitionKeyBuilder()
                    .Add(type)
                    .Add(bucket)
                    .Add(shortDate)
                    .Build();

                _logger.LogSpredInformation("CatalogAggregateReport", $"Saving chunk #{partIndex + 1} with {chunk.Count} items");

                await SafeCreateItemWithRetryAsync(_enrichmentContainer, partial, partitionKey, _logger);

                partIndex++;
            }

            _logger.LogSpredInformation("CatalogAggregateReport", $"Enrichment task completed: Id={id}, Parts={partIndex}");
        }
        catch (System.Exception ex)
        {
            _logger.LogSpredError("CatalogAggregateReport", $"Enrichment task failed for Id={id}", ex);
            throw;
        }
    }
    
    private async Task SafeCreateItemWithRetryAsync<T>(
        Container container,
        T item,
        PartitionKey partitionKey,
        ILogger logger,
        int maxRetries = 5)
    {
        int attempt = 0;
        var random = new Random();
        var delaysWithJitter = _delays
            .Select(d => d + TimeSpan.FromMilliseconds(d.TotalMilliseconds * (random.NextDouble() * 0.4 - 0.2)))
            .ToArray();

        while (true)
        {
            try
            {
                await container.CreateItemAsync(item, partitionKey);
                return;
            }
            catch (CosmosException ex) when (
                ex.StatusCode == HttpStatusCode.RequestTimeout ||     // 408
                ex.StatusCode == HttpStatusCode.TooManyRequests ||   // 429
                (int)ex.StatusCode >= 500                                       // 5xx
            )
            {
                attempt++;
                if (attempt > maxRetries)
                {
                    logger.LogSpredError("SafeCreate","Max retry attempts reached. Item save failed.", ex);
                    throw;
                }
                
                var delay = ex.RetryAfter ?? delaysWithJitter[Math.Min(attempt, _delays.Length - 1)];

                logger.LogSpredWarning("SafeCreate", $"Retry {attempt}: CosmosException {ex.StatusCode}, will retry after {delay}...");

                await Task.Delay(delay);
            }
            catch (System.Exception ex)
            {
                logger.LogSpredError("SafeCreate","Unexpected exception during CreateItemAsync.", ex);
                throw;
            }
        }
    }

    private async Task<CatalogInference> EnrichCatalogAsync(int bucket, Guid enrichmentId)
    {
        _logger.LogSpredInformation("EnrichCatalog", $"Starting enrichment: Id={enrichmentId}, Bucket={bucket}");

        var query = new QueryDefinition(@"
            SELECT * FROM c 
            WHERE c.Type = 'playlistMetadata' 
            ORDER BY c._ts DESC");

        var requestOptions = new QueryRequestOptions
        {
            PartitionKey = new PartitionKeyBuilder()
                .Add(Guid.Empty.ToString())
                .Add(bucket.ToString("D2"))
                .Build()
        };

        var results = new List<CatalogInferenceResponce>();
        using var iterator = _catalogContainer.GetItemQueryIterator<JObject>(query, requestOptions: requestOptions);

        int catalogsCount = 0;
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            foreach (var item in response)
            {
                var id = item["id"]?.ToString();
                var owner = item["SpredUserId"]?.ToString();
                var trackArray = item["Tracks"] as JArray;

                if (id == null || owner == null || trackArray == null) continue;

                var tracks = trackArray
                    .Select(x => Guid.TryParse(x.ToString(), out var guid) ? guid : Guid.Empty)
                    .Where(guid => guid != Guid.Empty)
                    .Select(guid => new TrackInference { TrackId = guid, TrackOwner = Guid.Empty })
                    .ToList();

                results.Add(new CatalogInferenceResponce
                {
                    CatalogId = Guid.Parse(id),
                    CatalogOwner = Guid.Parse(owner),
                    TrackIdOwner = tracks
                });

                catalogsCount++;
            }
        }

        _logger.LogSpredInformation("EnrichCatalog", $"Loaded catalogs: {catalogsCount}, Bucket: {bucket}");

        var allTrackIds = results
            .SelectMany(r => r.TrackIdOwner.Select(t => t.TrackId))
            .Distinct()
            .ToList();

        _logger.LogSpredInformation("EnrichCatalog", $"Extracted distinct track IDs: {allTrackIds.Count}, Bucket: {bucket}");

        var readItems = allTrackIds
            .Select(trackId => (
                id: trackId.ToString(),
                partitionKey: new PartitionKeyBuilder()
                    .Add(Guid.Empty.ToString())
                    .Add(GuidShortener.GenerateBucketFromGuid(trackId))
                    .Build()))
            .ToList();

        var genreByTrackId = new Dictionary<Guid, string>();
        var validTrackIds = new HashSet<Guid>();

        foreach (var batch in readItems.Chunk(100))
        {
            var response = await _trackContainer.ReadManyItemsAsync<JObject>(batch);

            foreach (var item in response.Resource)
            {
                if (!Guid.TryParse(item["id"]?.ToString(), out var trackId))
                    continue;

                var status = item["Status"]?.ToString();
                if (status != "1") continue;

                var genre = item["Genre"]?.ToString();
                genreByTrackId[trackId] = genre ?? string.Empty;
                validTrackIds.Add(trackId);
            }
        }

        _logger.LogSpredInformation("EnrichCatalog", $"Valid tracks: {validTrackIds.Count}, Skipped: {allTrackIds.Count - validTrackIds.Count}, Bucket: {bucket}");

        foreach (var catalog in results)
        {
            catalog.TrackIdOwner = catalog.TrackIdOwner
                .Where(t => validTrackIds.Contains(t.TrackId))
                .Select(t =>
                {
                    genreByTrackId.TryGetValue(t.TrackId, out var genre);
                    t.Genre = genre ?? string.Empty;
                    return t;
                })
                .ToList();
        }

        _logger.LogSpredInformation("EnrichCatalog", $"Final catalog count after filtering: {results.Count}, Bucket: {bucket}");

        return new CatalogInference
        {
            id = enrichmentId,
            Type = "playlistMetadata",
            Bucket = bucket,
            catalogInferenceResponces = results,
            EnrichedAt = DateTime.UtcNow
        };
    }
}
