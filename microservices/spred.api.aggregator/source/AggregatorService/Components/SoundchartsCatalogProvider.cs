using System.Text.Json;
using AggregatorService.Abstractions;
using AggregatorService.Configurations;
using AggregatorService.Extensions;
using AggregatorService.Models;
using AggregatorService.Models.Dto;
using AutoMapper;
using Extensions.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Refit;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;

namespace AggregatorService.Components;

/// <inheritdoc />
public class SoundchartsCatalogProvider : ICatalogProvider
{
    private readonly ISoundchartsApi _api;
    private readonly IMapper _mapper;
    private readonly IApiRateLimiter _rateLimiter;
    private readonly string _appId;
    private readonly string _apiKey;
    private readonly ILogger<SoundchartsCatalogProvider> _logger;
    private readonly IMemoryCache _cache;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly TimeSpan _ttl = TimeSpan.FromHours(12);

    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions()
    {
        PropertyNameCaseInsensitive = true
    };
    
    private static readonly SemaphoreSlim _semaphore = new(100);

    /// <summary>
    /// Initializes a new instance of the <see cref="SoundchartsCatalogProvider"/> class.
    /// </summary>
    /// <param name="api">Soundcharts API client.</param>
    /// <param name="mapper">AutoMapper instance.</param>
    /// <param name="cache">Memory cache.</param>
    /// <param name="options">Soundcharts API credentials.</param>
    /// <param name="factory">Logger factory.</param>
    /// <param name="rateLimiter">Rate limiter.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="api"/>, <paramref name="mapper"/>, or <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="options"/> is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="options"/> is invalid.</exception>
    public SoundchartsCatalogProvider(
        ISoundchartsApi api,
        IMapper mapper,
        IMemoryCache cache,
        IOptions<SoundchartsOptions> options, ILoggerFactory factory, IApiRateLimiter rateLimiter)
    {
        _api = api;
        _mapper = mapper;
        _rateLimiter = rateLimiter;
        _appId = options.Value.AppId;
        _apiKey = options.Value.ApiKey;
        _logger = factory.CreateLogger<SoundchartsCatalogProvider>();
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<string?> ResolvePlaylistIdAsync(string primaryId, string platform)
    {
        var identifier = primaryId.Split(':').LastOrDefault();
        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        var response = await ExecuteSafeAsync(
            () => _api.GetPlaylistByPlatformIdAsync(_appId, _apiKey, platform, identifier),
            "ResolvePlaylistId",
            identifier);

        return response?.Content.TryGetValue("object").TryGetValue("uuid").GetStringOrNull();
    }

    /// <inheritdoc />
    public async Task<MetadataDto?> GetPlaylistMetadataAsync(string playlistId, string platform)
    {
        var response = await ExecuteSafeAsync(
            () => _api.GetPlaylistMetadataAsync(_appId, _apiKey, playlistId),
            "GetPlaylistMetadata",
            playlistId);

        if (response == null) return null;

        var wrapper = new SoundchartsPlaylistWrapper { Data = response.Content };
        return _mapper.Map<MetadataDto>(wrapper);
    }

    /// <inheritdoc />
    public async Task<HashSet<StatInfo>> GetPlaylistStatsAsync(string playlistId, string platform, bool updateStats)
    {
        if (!updateStats) return [];

        var response = await ExecuteSafeAsync(
            () => _api.GetPlaylistAudienceAsync(playlistId, _appId, _apiKey),
            "GetPlaylistAudience",
            playlistId);

        if (response == null || !response.IsSuccessful || response.Content.ValueKind == JsonValueKind.Undefined)
            return [];

        var items = response.Content
            .GetProperty("items")
            .EnumerateArraySafe()
            .Select(x => new StatInfo
            {
                Timestamp = x.GetProperty("date").GetDateTime(),
                Value = x.GetProperty("value").GetUInt32()
            })
            .ToHashSet();

        return items;
    }
    /// <inheritdoc />
    public async Task<List<TrackDtoWithPlatformIds>> GetPlaylistTracksSnapshotAsync(string playlistId, string platform, DateTime reportDate)
    {
        var response = await ExecuteSafeAsync(
            () => _api.GetPlaylistTracksLatestAsync(_appId, _apiKey, playlistId),
            "GetPlaylistTracks",
            playlistId);

        if (response == null) return [];

        var items = response.Content
            .TryGetValue("items")
            .EnumerateArraySafe()
            .Where(item => item.TryGetValue("exitDate").GetDateTimeOrDefault(DateTime.MinValue) == DateTime.MinValue)
            .Select(item => new
            {
                Uuid = item.TryGetValue("song").TryGetValue("uuid").GetStringOrNull(),
                EntryDate = item.TryGetValue("entryDate").GetDateTimeOrDefault(DateTime.MinValue),
                ExitDate = item.TryGetValue("exitDate").GetDateTimeOrDefault(DateTime.MinValue)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Uuid))
            .ToList();

        var tasks = items.Select(x => ProcessTrackAsync(x.Uuid!, x.EntryDate));
        var results = await Task.WhenAll(tasks);

        return results
            .Where(dto => dto != null)
            .ToList()!;
    }

    public async Task<RadioInfo?> GetRadioMetadataAsync(string slug)
    {
        if (_cache.TryGetValue(slug, out RadioInfo? cached) && cached != null)
        {
            _logger.LogSpredDebug("Soundcharts.RadioMetadata", $"Cache hit for {slug}");
            return cached;
        }

        await _refreshLock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(slug, out cached) && cached != null)
            {
                _logger.LogSpredDebug("Soundcharts.RadioMetadata", $"Cache hit for {slug} after lock acquisition");
                return cached;
            }

            _logger.LogSpredInformation("Soundcharts.RadioMetadata", $"Cache miss for {slug}, refreshing full radio catalog");

            var allRadios = new List<RadioInfo>();
            var offset = 0;
            const int limit = 100;
            var total = int.MaxValue;
            
            while (offset < total)
            {
                var response = await _api.GetRadiosStats(_appId, _apiKey, offset, limit);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogSpredWarning("Soundcharts.RadioMetadata", $"Failed to fetch radio page {offset}, status {response.StatusCode}");
                }
                
                var json = response.Content;
                if (json.TryGetProperty("items", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        var dto = JsonSerializer.Deserialize<RadioInfo>(
                            item.GetRawText(), _jsonSerializerOptions);

                        if (dto != null && !string.IsNullOrWhiteSpace(dto.Slug))
                        {
                            _cache.Set(dto.Slug, dto, _ttl);
                            allRadios.Add(dto);
                        }
                    }
                }
                
                if (json.TryGetProperty("page", out var page))
                {
                    if (page.TryGetProperty("total", out var totalProp) && totalProp.TryGetInt32(out var totalVal))
                        total = totalVal;

                    if (page.TryGetProperty("next", out var nextProp) && nextProp.ValueKind != JsonValueKind.Null)
                    {
                        offset += limit;
                    }
                    else
                        break;
                }
                else
                    break;
            }

            _logger.LogSpredInformation("Soundcharts.RadioMetadata", $"Cached {allRadios.Count} radio entries (TTL: {_ttl.TotalHours}h)");
            return allRadios.FirstOrDefault(r => r.Slug == slug);
        }
        catch (System.Exception ex)
        {
            _logger.LogSpredError("Soundcharts.RadioMetadata","Exception while refreshing radio catalog", ex);
            return null;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<(List<TrackDtoWithPlatformIds>, int)> GetRadioTracksSnapshotAsync(string slug, int trackLimit)
    {
        _logger.LogSpredInformation("Soundcharts.RadioTracks", $"Fetching latest radio tracks for {slug}");

        var response = await _api.GetRadioTracksLatestAsync(_appId, _apiKey, slug);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogSpredWarning("Soundcharts.RadioTracks", $"Failed to fetch latest tracks for {slug}, status {response.StatusCode}");
            return ([], 0);
        }

        var json = response.Content;
        if (!json.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            _logger.LogSpredWarning("Soundcharts.RadioTracks", $"No 'items' array found for {slug}");
            return ([], 0);
        }

        int total = 0;
        if (json.TryGetProperty("page", out var pageElement) &&
            pageElement.TryGetProperty("total", out var totalElement))
        {
            total = totalElement.GetInt32();
        }

        var trackPairs = new List<(string Uuid, DateTimeOffset? AiredAt)>();
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("song", out var song) ||
                !song.TryGetProperty("uuid", out var uuidProp))
                continue;

            var uuid = uuidProp.GetString();
            if (string.IsNullOrWhiteSpace(uuid))
                continue;

            DateTimeOffset? airedAt = null;
            if (item.TryGetProperty("airedAt", out var airedAtProp) &&
                airedAtProp.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(airedAtProp.GetString(), out var parsed))
            {
                airedAt = parsed;
            }

            trackPairs.Add((uuid, airedAt));
        }

        _logger.LogSpredDebug("Soundcharts.RadioTracks", $"Found {trackPairs.Count} unique track UUIDs for {slug}");
        var limitedUuids = trackPairs.Take(trackLimit).ToList();

        var tracks = new List<TrackDtoWithPlatformIds>();
        foreach (var uuid in limitedUuids)
        {
            try
            {
                var track = await ProcessTrackAsync(uuid.Uuid, uuid.AiredAt?.Date ?? DateTime.Now);
                if (track != null)
                    tracks.Add(track);
            }
            catch (System.Exception ex)
            {
                _logger.LogSpredError("Soundcharts.RadioTracks", $"Failed to process track {uuid}", ex);
            }
        }

        _logger.LogSpredInformation("Soundcharts.RadioTracks", $"Processed {tracks.Count}/{limitedUuids.Count} tracks for {slug}");
        if(total == 0)
            total = tracks.Count;
        
        return (tracks, total);
    }

    public async Task<List<(string Platform, string PrimaryId, string Url)>> GetRadioPlatforms(string slug)
    {
        _logger.LogSpredInformation("Soundcharts.RadioPlatforms", $"Fetching related platforms for {slug}");

        var response = await _api.GetRadioUrlPlatforms(_appId, _apiKey, slug);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogSpredWarning("Soundcharts.RadioPlatforms", $"Failed to fetch related platforms for {slug}, status {response.StatusCode}");
            return [];
        }

        var json = response.Content;

        if (!json.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            _logger.LogSpredWarning("Soundcharts.RadioPlatforms", $"No 'items' array found for {slug}");
            return [];
        }

        var platforms = new List<(string Platform, string PrimaryId, string Url)>();

        foreach (var item in items.EnumerateArray())
        {
            var platform = item.TryGetProperty("platformCode", out var platformProp)
                ? platformProp.GetString() ?? string.Empty
                : string.Empty;

            var primaryId = item.TryGetProperty("identifier", out var idProp)
                ? idProp.GetString() ?? string.Empty
                : string.Empty;

            var url = item.TryGetProperty("url", out var urlProp)
                ? urlProp.GetString() ?? string.Empty
                : string.Empty;

            platforms.Add((platform, primaryId, url));
        }

        _logger.LogSpredDebug("Soundcharts.RadioPlatforms", $"Fetched {platforms.Count} platform entries for {slug}");
        return platforms;
    }

    private async Task<TrackDtoWithPlatformIds?> ProcessTrackAsync(string uuid, DateTime entryDate)
    {
        var songResponse = await ExecuteSafeAsync(
            () => _api.GetSongByUuidAsync(_appId, _apiKey, uuid),
            "GetSongByUuid",
            uuid);

        if (songResponse == null) return null;

        var wrapper = new SoundchartsTrackWrapper
        {
            Data = songResponse.Content
        };

        var track = _mapper.Map<TrackDtoWithPlatformIds>(wrapper);

        track.AddedAt = entryDate;

        var identifiersResponse = await ExecuteSafeAsync(
            () => _api.GetSongIdentifiersAsync(uuid, _appId, _apiKey, CancellationToken.None),
            "GetSongIdentifiers",
            uuid);

        if (identifiersResponse is { IsSuccessful: true, Content.ValueKind: JsonValueKind.Object })
        {
            var (ids, urls) = ToPlatformIdPairs(identifiersResponse.Content);
            track.PrimaryIds = ids;
            track.TrackUrl = urls;
        }
        else
            _logger.LogSpredWarning(
                "GetSongIdentifiers", 
                $"Identifiers not found or invalid for track {uuid}");

        return track;
    }
    
    private static (List<PlatformIdPair> Ids, List<PlatformUrl> Urls) ToPlatformIdPairs(JsonElement element)
    {
        var ids = new List<PlatformIdPair>();
        var urls = new List<PlatformUrl>();

        if (element.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                var platform   = item.GetProperty("platformCode").GetString() ?? string.Empty;
                var identifier = item.GetProperty("identifier").GetString() ?? string.Empty;
                var urlString  = item.GetProperty("url").GetString();

                ids.Add(new PlatformIdPair(platform, identifier));

                if (!string.IsNullOrWhiteSpace(urlString) && Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
                {
                    urls.Add(new PlatformUrl { Platform = platform, Value = uri });
                }
            }
        }

        return (ids, urls);
    }
    
    private async Task<IApiResponse<JsonElement>?> ExecuteSafeAsync(
        Func<Task<IApiResponse<JsonElement>>> action,
        string context,
        string id,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var response = await _rateLimiter.ExecuteAsync(action);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogSpredWarning(context, $"Soundcharts API error for {id}: HTTP {response.StatusCode}");
                return null;
            }

            if (response.Content.ValueKind == JsonValueKind.Undefined)
            {
                _logger.LogSpredWarning(context, $"Empty content for {id}");
                return null;
            }

            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogSpredError(context, $"Daily rate limit exhausted for Soundcharts", ex);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
