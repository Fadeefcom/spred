using System.Text.Json;
using Refit;

namespace AggregatorService.Abstractions;

public interface ISoundchartsApi
{
    [Get("/api/v2.20/playlist/by-curator/{platform}/{curatorIdentifier}")]
    Task<IApiResponse<JsonElement>> GetPlaylistsByCuratorAsync(
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        string platform,
        string curatorIdentifier,
        [Query] string countryCode = null,
        [Query] int? offset = null,
        [Query] int? limit = null,
        [Query] string sortBy = null,
        [Query] string sortOrder = null);

    [Get("/api/v2/playlist/curators/{platform}")]
    Task<IApiResponse<JsonElement>> GetCuratorsByPlatformAsync(
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        string platform,
        [Query] int? offset = null,
        [Query] int? limit = null);

    [Get("/api/v2.8/playlist/by-platform/{platform}/{identifier}")]
    Task<IApiResponse<JsonElement>> GetPlaylistByPlatformIdAsync(
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        string platform,
        string identifier,
        [Query] string countryCode = null);

    [Get("/api/v2.8/playlist/{uuid}")]
    Task<IApiResponse<JsonElement>> GetPlaylistMetadataAsync(
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        string uuid);

    [Get("/api/v2.20/playlist/{uuid}/tracks/latest")]
    Task<IApiResponse<JsonElement>> GetPlaylistTracksLatestAsync(
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        string uuid);

    [Get("/api/v2.20/playlist/{uuid}/available-tracklistings")]
    Task<IApiResponse<JsonElement>> GetPlaylistTracklistingDatesAsync(
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        string uuid,
        [Query] string endDate = null,
        [Query] int? period = null,
        [Query] int? offset = null,
        [Query] int? limit = null);

    [Get("/api/v2.20/playlist/{uuid}/tracks/{datetime}")]
    Task<IApiResponse<JsonElement>> GetPlaylistTracksForDateAsync(
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        string uuid,
        string datetime,
        [Query] int? offset = null,
        [Query] int? limit = null);

    [Post("/api/v2/top/playlists/{platform}")]
    Task<IApiResponse<JsonElement>> GetPlaylistsTopAsync(
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        string platform,
        [Query] int? offset = null,
        [Query] int? limit = null,
        [Body] object body = null);
    
    [Get("/api/v2.25/song/{uuid}")]
    Task<IApiResponse<JsonElement>> GetSongByUuidAsync(
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        string uuid);

    [Get("/api/v2/search/external/url")]
    Task<IApiResponse<JsonElement>> SearchByExternalUrlAsync(
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        [Query] string platformUrl);
    
    [Get("/api/v2/song/{uuid}/identifiers")]
    Task<IApiResponse<JsonElement>> GetSongIdentifiersAsync(
        string uuid,
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        CancellationToken cancellationToken = default);
    
    [Get("/api/v2.20/playlist/{uuid}/available-tracklistings")]
    Task<IApiResponse<JsonElement>> GetPlaylistAvailableTrackListingsAsync(
        string uuid,
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        CancellationToken cancellationToken = default);

    [Get("/api/v2.20/playlist/{uuid}/tracklisting/{reportDate}")]
    Task<IApiResponse<JsonElement>> GetPlaylistMetadataAtDateAsync(
        string uuid,
        string reportDate,
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        CancellationToken cancellationToken = default);
    
    [Get("/api/v2.20/playlist/{uuid}/audience")]
    Task<IApiResponse<JsonElement>> GetPlaylistAudienceAsync(
        string uuid,
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        CancellationToken cancellationToken = default);
    
    [Get("/api/v2/radio/{slug}/live-feed")]
    Task<IApiResponse<JsonElement>> GetRadioTracksLatestAsync(
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        string slug,
        [Query] int? offset = null,
        [Query] int? limit = 100);
    
    [Get("/api/v2/radio/{slug}/identifiers")]
    Task<IApiResponse<JsonElement>> GetRadioUrlPlatforms(
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        string slug,
        [Query] int? offset = null,
        [Query] int? limit = 100);
    
    [Get("/api/v2.22/radio")]
    Task<IApiResponse<JsonElement>> GetRadiosStats(
        [Header("x-app-id")] string appId,
        [Header("x-api-key")] string apiKey,
        [Query] int? offset = null,
        [Query] int? limit = 100);
}