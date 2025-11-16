using System.Text.Json;
using AggregatorService.Models.Dto;
using Refit;

namespace AggregatorService.Abstractions;

/// <summary>
/// Defines the Chartmetrics API contract used to authenticate and retrieve playlist data,
/// statistics, and historical snapshots.
/// </summary>
public interface IChartmetricsApi
{
    /// <summary>
    /// Retrieves a new access token using the provided refresh token or credentials.
    /// </summary>
    /// <param name="request">The token request payload.</param>
    /// <returns>An <see cref="ApiResponse{T}"/> containing the access token as a JSON object.</returns>
    [Post("/api/token")]
    public Task<IApiResponse<JsonElement>> GetAccessToken(ChartTokenRequest request);

    /// <summary>
    /// Searches for a playlist by its Spotify URL.
    /// </summary>
    /// <param name="bearerToken">The Bearer token used for authorization.</param>
    /// <param name="playlistId">The playlist ID embedded in the Spotify URL.</param>
    /// <returns>An <see cref="ApiResponse{T}"/> containing search results.</returns>
    [Get("/api/search?type=playlists&q=https://open.spotify.com/playlist/{playlistId}")]
    public Task<IApiResponse<JsonElement>> SearchPlaylistId([Header("Authorization")] string bearerToken, string playlistId);

    /// <summary>
    /// Retrieves metadata for a playlist by platform and playlist ID.
    /// </summary>
    /// <param name="bearerToken">The Bearer token used for authorization.</param>
    /// <param name="playlistId">The unique identifier of the playlist.</param>
    /// <param name="platform">The platform name (e.g., "spotify", "applemusic").</param>
    /// <returns>An <see cref="ApiResponse{T}"/> containing playlist metadata.</returns>
    [Get("/api/playlist/{platform}/{playlistId}")]
    public Task<IApiResponse<JsonElement>> GetPlaylist([Header("Authorization")] string bearerToken, string playlistId,
        string platform);

    /// <summary>
    /// Retrieves time-series statistics (e.g., follower history) for a specific playlist.
    /// </summary>
    /// <param name="bearerToken">The Bearer token used for authorization.</param>
    /// <param name="playlistId">The playlist's unique identifier.</param>
    /// <param name="platform">The platform on which the playlist is hosted.</param>
    /// <returns>An <see cref="ApiResponse{T}"/> containing playlist statistics.</returns>
    [Get("/api/playlist/{platform}/{playlistId}/stats")]
    public Task<IApiResponse<JsonElement>> GetStatsPlaylist([Header("Authorization")] string bearerToken, string playlistId,
        string platform);

    /// <summary>
    /// Retrieves a historical snapshot for a playlist on a specific date.
    /// </summary>
    /// <param name="bearerToken">The Bearer token used for authorization.</param>
    /// <param name="playlistId">The playlist ID to query.</param>
    /// <param name="platform">The target platform name.</param>
    /// <param name="shortDate">The date in yyyy-MM-dd format (e.g., 2025-06-21).</param>
    /// <returns>An <see cref="ApiResponse{T}"/> containing the snapshot data.</returns>
    [Get("/api/playlist/{platform}/{playlistId}/snapshot?date={shortDate}")]
    public Task<IApiResponse<JsonElement>> GetSnapshot([Header("Authorization")] string bearerToken, string playlistId,
        string platform, string shortDate);
}