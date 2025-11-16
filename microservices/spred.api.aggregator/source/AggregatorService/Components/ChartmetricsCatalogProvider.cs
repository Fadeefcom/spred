using System.Text.Json;
using AggregatorService.Abstractions;
using AggregatorService.Extensions;
using AggregatorService.Models;
using AggregatorService.Models.Dto;
using AutoMapper;
using Extensions.Extensions;
using Refit;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;

namespace AggregatorService.Components;

/// <summary>
/// Provides a data access layer to the Chartmetric API, 
/// supporting rate limiting, authentication, error handling, and AutoMapper integration.
/// </summary>
public sealed class ChartmetricsCatalogProvider : ICatalogProvider
{
    private readonly IChartmetricsApi _api;
    private readonly IChartmetricsTokenProvider _tokenProvider;
    private readonly IApiRateLimiter _rateLimiter;
    private readonly IMapper _mapper;
    private readonly ILogger<ChartmetricsCatalogProvider> _logger;
    
    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="api"></param>
    /// <param name="tokenProvider"></param>
    /// <param name="rateLimiter"></param>
    /// <param name="mapper"></param>
    /// <param name="logger"></param>
    public ChartmetricsCatalogProvider(
        IChartmetricsApi api,
        IChartmetricsTokenProvider tokenProvider,
        IApiRateLimiter rateLimiter,
        IMapper mapper,
        ILogger<ChartmetricsCatalogProvider> logger)
    {
        _api = api;
        _tokenProvider = tokenProvider;
        _rateLimiter = rateLimiter;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> ResolvePlaylistIdAsync(string primaryId, string platform)
    {
        var token = await _tokenProvider.GetAccessTokenAsync();

        var response = await ExecuteSafeAsync(
            () => _api.SearchPlaylistId($"Bearer {token}", primaryId),
            "ResolvePlaylistId",
            primaryId
        );

        var id = TryExtractPlaylistId(response, platform);
        return id?.ToString();
    }

    /// <inheritdoc />
    public async Task<MetadataDto?> GetPlaylistMetadataAsync(string playlistId, string platform)
    {
        var token = await _tokenProvider.GetAccessTokenAsync();

        var response = await ExecuteSafeAsync(
            () => _api.GetPlaylist($"Bearer {token}", playlistId, platform),
            "GetPlaylistMetadata",
            playlistId
        );

        if (response?.Content.ValueKind != JsonValueKind.Object)
            return null;
        
        var snapshot = _mapper.Map<MetadataDto>(response.Content.GetProperty("obj")); 
        return snapshot;
    }

    /// <inheritdoc />
    public async Task<HashSet<StatInfo>> GetPlaylistStatsAsync(string playlistId, string platform, bool updateStats)
    {
        if (!updateStats)
            return [];

        var token = await _tokenProvider.GetAccessTokenAsync();

        var response = await ExecuteSafeAsync(
            () => _api.GetStatsPlaylist($"Bearer {token}", playlistId, platform),
            "GetPlaylistStats",
            playlistId
        );

        if (response?.Content.ValueKind != JsonValueKind.Object)
            return [];

        var statsArray = response.Content
            .TryGetValue("obj")
            .EnumerateArraySafe()
            .ToList();

        return _mapper.Map<HashSet<StatInfo>>(statsArray);
    }

    /// <inheritdoc />
    public async Task<List<TrackDtoWithPlatformIds>> GetPlaylistTracksSnapshotAsync(string playlistId, string platform, DateTime reportDate)
    {
        var token = await _tokenProvider.GetAccessTokenAsync();

        var response = await ExecuteSafeAsync(
            () => _api.GetSnapshot($"Bearer {token}", playlistId, platform, reportDate.ToString("yyyy-MM-dd")),
            "GetPlaylistTracksSnapshot",
            playlistId
        );

        if (response?.Content.ValueKind != JsonValueKind.Object)
            return [];

        var wrappers = response.Content
            .TryGetValue("obj")
            .EnumerateArraySafe()
            .Select(el => new ChartmetricsTrackWrapper { Data = el })
            .ToList();

        return _mapper.Map<List<TrackDtoWithPlatformIds>>(wrappers);
    }

    /// <inheritdoc />
    public Task<RadioInfo?> GetRadioMetadataAsync(string slug)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<(List<TrackDtoWithPlatformIds>, int)> GetRadioTracksSnapshotAsync(string slug, int trackLimit)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task<List<(string Platform, string PrimaryId, string Url)>> GetRadioPlatforms(string slug)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Executes Chartmetric API call with rate limit and error handling.
    /// </summary>
    private async Task<IApiResponse<JsonElement>?> ExecuteSafeAsync(
        Func<Task<IApiResponse<JsonElement>>> apiCall,
        string context,
        string id)
    {
        try
        {
            var response = await _rateLimiter.ExecuteAsync(apiCall);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogSpredWarning(context, $"Chartmetrics API error for {id}: HTTP {response.StatusCode}");
                return null;
            }

            if (response.Content.ValueKind == JsonValueKind.Undefined)
            {
                _logger.LogSpredWarning(context, $"Chartmetrics API returned empty content for {id}");
                return null;
            }

            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogSpredError(context, $"Chartmetrics rate limit exceeded — aborting execution for {id}", ex);
            throw;
        }
        catch (System.Exception ex)
        {
            _logger.LogSpredError(context, $"Unexpected error in Chartmetrics API for {id}", ex);
            return null;
        }
    }

    private static long? TryExtractPlaylistId(IApiResponse<JsonElement>? response, string platform)
    {
        if (response?.Content.ValueKind != JsonValueKind.Object)
            return null;

        var playlists = response.Content
            .TryGetValue("obj")
            .TryGetValue("playlists")
            .TryGetValue(platform);

        var first = playlists.EnumerateArraySafe().FirstOrDefault();
        var idElement = first.TryGetValue("id");

        return idElement?.ValueKind == JsonValueKind.Number && idElement.Value.TryGetInt64(out var id)
            ? id
            : null;
    }
}
