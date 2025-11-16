using System.Diagnostics;
using System.Text.Json;
using Extensions.Extensions;
using InferenceService.Abstractions;
using InferenceService.Configuration;
using InferenceService.Helpers;
using InferenceService.Models.Dto;
using InferenceService.Models.Entities;
using MassTransit;
using Microsoft.Extensions.Options;
using Spred.Bus.Contracts;
using StackExchange.Redis;

namespace InferenceService.Components.Consumers;

/// <summary>
/// Processes ready-made audio embeddings delivered via <see cref="TrackEmbeddingResult"/>:
/// performs a vector search against the catalog, persists inference results, updates transient
/// state in Redis, and publishes a <see cref="Spred.Bus.Contracts.TrackUpdateRequest"/> with detected genres.
/// </summary>
/// <remarks>
/// This consumer does not perform audio decoding or model inference. It assumes embeddings are already computed
/// upstream and focuses solely on: (1) vector DB query, (2) persistence via <c>IInferenceManager</c>,
/// (3) Redis status tracking, and (4) follow-up event publication.
/// </remarks>
public sealed class InferenceEmbeddingConsumer : IConsumer<TrackEmbeddingResult>
{
    private readonly ILogger<InferenceEmbeddingConsumer> _logger;
    private readonly IDatabase _database;
    private readonly IVectorSearch _vectorSearch;
    private readonly IInferenceManager _inferenceManager;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly ModelVersion _modelVersion;

    /// <summary>
    /// Initializes a new instance configured to query the vector store and persist inference outcomes.
    /// </summary>
    /// <param name="loggerFactory">Factory used to create an <see cref="Microsoft.Extensions.Logging.ILogger"/> for this consumer.</param>
    /// <param name="connectionMultiplexer">Redis multiplexer used to obtain a database for transient state updates.</param>
    /// <param name="vectorSearch">Vector search API used to find similar catalogs/tracks by embedding.</param>
    /// <param name="inferenceManager">Service that saves inference results to the persistence layer.</param>
    /// <param name="sendEndpointProvider">MassTransit send provider to publish follow-up update messages.</param>
    /// <param name="modelVersion">Active model configuration (thresholds) used for search parameters and cache key composition.</param>
    /// <remarks>
    /// The <paramref name="modelVersion"/> is used to align search thresholds and to compose the Redis cache key,
    /// enabling per-model version deduplication and status tracking.
    /// </remarks>
    public InferenceEmbeddingConsumer(
        ILoggerFactory loggerFactory,
        IConnectionMultiplexer connectionMultiplexer,
        IVectorSearch vectorSearch,
        IInferenceManager inferenceManager,
        ISendEndpointProvider sendEndpointProvider,
        IOptions<ModelVersion> modelVersion)
    {
        _logger = loggerFactory.CreateLogger<InferenceEmbeddingConsumer>();
        _database = connectionMultiplexer.GetDatabase();
        _vectorSearch = vectorSearch;
        _inferenceManager = inferenceManager;
        _sendEndpointProvider = sendEndpointProvider;
        _modelVersion = modelVersion.Value;
    }

    /// <summary>
    /// Consumes a <see cref="TrackEmbeddingResult"/> message and executes vector search + persistence workflow.
    /// </summary>
    /// <param name="context">MassTransit consumption context containing the <see cref="TrackEmbeddingResult"/> payload.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// Processing flow:
    /// 1) Mark <c>received</c> in Redis under key <c>rabbit:{TrackId}:{SpredUserId}:{ModelVersion}</c>.
    /// 2) If <c>Success == false</c>, mark <c>failed</c> and stop.
    /// 3) Query vector DB with <see cref="SearchQuery"/> using thresholds from <c>ModelVersion</c>.
    /// 4) Map search JSON into domain models and persist via <see cref="InferenceService.Abstractions.IInferenceManager"/>.
    /// 5) Mark <c>completed</c> in Redis and send <see cref="Spred.Bus.Contracts.TrackUpdateRequest"/> with aggregated genres.
    /// </remarks>
    /// <exception cref="System.NotSupportedException">
    /// Thrown when the vector search response has an unexpected JSON shape (e.g., non-array where array expected).
    /// </exception>
    /// <exception cref="System.Exception">
    /// Propagates unexpected errors after marking the current operation as <c>failed</c> in Redis.
    /// </exception>
    public async Task Consume(ConsumeContext<TrackEmbeddingResult> context)
    {
        var msg = context.Message;
        var cacheKey = $"inference:{msg.TrackId}:{msg.SpredUserId}";
        var globalStart = Stopwatch.StartNew();

        try
        {
            await _database.StringSetAsync(cacheKey, "received", TimeSpan.FromMinutes(15), When.Always, CommandFlags.None);

            if (!msg.Success)
            {
                await _database.StringSetAsync(cacheKey, "failed", TimeSpan.FromMinutes(15), When.Exists, CommandFlags.FireAndForget);
                _logger.LogSpredWarning("Embedding input", $"Failed embedding: {msg.ErrorMessage ?? "unknown error"} for TrackId={msg.TrackId}");
                return;
            }

            var sw = Stopwatch.StartNew();
            var searchResult = await _vectorSearch.SearchCatalogs(new SearchQuery
            {
                Embedding = msg.Embedding,
                SimilarityThreshold = _modelVersion.Threshold
            });
            _logger.LogSpredDebug("Elapsed", $"[Timing] Vector search: {sw.ElapsedMilliseconds} ms");

            if (!searchResult.IsSuccessful)
            {
                await _database.StringSetAsync(cacheKey, "failed", TimeSpan.FromMinutes(15), When.Exists, CommandFlags.FireAndForget);
                _logger.LogSpredDebug("Processing result", $"Search catalogs failed, {searchResult.StatusCode}:{searchResult.ReasonPhrase}");
                return;
            }

            var (inferenceList, similarTracksOverall, genres) = MapToInferenceMetadataList(searchResult.Content);

            await _inferenceManager.SaveInference(inferenceList, msg.TrackId, msg.SpredUserId, _modelVersion.Version, CancellationToken.None);

            await _database.StringSetAsync(cacheKey, "completed", TimeSpan.FromMinutes(15), When.Exists, CommandFlags.FireAndForget);

            _logger.LogSpredInformation("Completed embedding request", $"TrackId={msg.TrackId}, send track aggregator {similarTracksOverall.Count}");
            var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("exchange:track-update-request"));
            await endpoint.Send(new TrackUpdateRequest
            {
                TrackId = msg.TrackId,
                SpredUserId = msg.SpredUserId,
                Genre = genres
            }, CancellationToken.None);
        }
        catch (System.Exception ex)
        {
            await _database.StringSetAsync(cacheKey, "failed", TimeSpan.FromMinutes(15), When.Exists, CommandFlags.FireAndForget);
            _logger.LogSpredError("InferenceEmbedding", "Processing failed", ex);
            throw;
        }
        finally
        {
            _logger.LogSpredDebug("Elapsed", $"Total processing time: {globalStart.ElapsedMilliseconds} ms");
        }
    }

    private (List<InferenceMetadata>, List<SimilarTrack>, string) MapToInferenceMetadataList(JsonElement json)
    {
        var list = new List<InferenceMetadata>();

        if (json.TryGetProperty("results", out var catalogElements))
        {
            if (catalogElements.ValueKind != JsonValueKind.Array)
                throw new NotSupportedException("Invalid JSON format");

            foreach (var item in catalogElements.EnumerateArray())
            {
                var metadata = new InferenceMetadata
                {
                    MetadataId = Guid.Parse(item.GetProperty("catalogId").GetString()!),
                    MetadataOwner = Guid.Parse(item.GetProperty("catalogOwner").GetString()!),
                    Type = CatalogTypeHelper.NormalizeCatalogType(item.GetProperty("catalogType").GetString()),
                    Score = item.GetProperty("score").GetSingle() / 100,
                    Reaction = new ReactionStatus(),
                    SimilarTracks =
                        item.TryGetProperty("topnSimilarTracks", out var tracks) && tracks.ValueKind == JsonValueKind.Array
                            ? tracks.EnumerateArray().Select(track => new SimilarTrack
                            {
                                SimilarTrackId = Guid.Parse(track.GetProperty("trackId").GetString()!),
                                TrackOwner = Guid.Parse(track.GetProperty("trackOwner").GetString()!),
                                Similarity = track.GetProperty("similarityScore").GetSingle()
                            }).ToList()
                            : []
                };

                list.Add(metadata);
            }
        }

        var genreResult = string.Empty;
        if (json.TryGetProperty("genres", out var genres))
        {
            var genreList = genres.EnumerateArray()
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrEmpty(x));
            genreResult = string.Join(", ", genreList);
        }

        List<SimilarTrack> similarTracks = [];
        if (json.TryGetProperty("topnSimilarTracksOverall", out var tracksElements))
        {
            similarTracks = tracksElements.ValueKind == JsonValueKind.Array
                ? tracksElements.EnumerateArray().Select(track => new SimilarTrack
                {
                    SimilarTrackId = Guid.Parse(track.GetProperty("trackId").GetString()!),
                    TrackOwner = Guid.Parse(track.GetProperty("trackOwner").GetString()!),
                    Similarity = track.GetProperty("similarityScore").GetSingle()
                }).ToList()
                : [];
        }

        return (list, similarTracks, genreResult);
    }
}