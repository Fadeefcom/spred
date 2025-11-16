using System.Text.Json;
using Refit;

namespace AggregatorService.Abstractions;

/// <summary>
/// Spotify web hooks
/// </summary>
public interface ISpotifyApi
{
    [Get("/users/{userId}/playlists?limit={limit}&offset={offset}")]
    Task<IApiResponse<JsonElement>> GetUserPlaylists([Header("Authorization")] string bearerToken, string userId, int limit, int offset);

    [Get("/playlists/{primaryId}/tracks?limit={limit}&offset={offset}")]
    Task<IApiResponse<JsonElement>> GetPlaylistTracks([Header("Authorization")] string bearerToken, string primaryId, int limit, int offset);

    [Get("/playlists/{playlistId}")]
    Task<IApiResponse<JsonElement>> GetPlaylist([Header("Authorization")] string bearerToken, string playlistId);
}