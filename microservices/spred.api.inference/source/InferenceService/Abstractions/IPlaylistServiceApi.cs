using System.Text.Json;
using Refit;

namespace InferenceService.Abstractions;

/// <summary>
/// Playlist service routes
/// </summary>
public interface IPlaylistServiceApi
{
    /// <summary>
    /// Get playlist by ID
    /// </summary>
    /// <param name="bearerToken"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [Get("/internal/playlist/{id}")]
    public Task<ApiResponse<JsonElement>> GetPlaylistById([Header("Authorization")] string bearerToken, string id);
}