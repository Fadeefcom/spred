using System.Text.Json;
using Refit;
using Spred.Bus.DTOs;

namespace PlaylistService.Abstractions;

/// <summary>
/// Track harbor endpoint
/// </summary>
public interface ITrackServiceApi
{
    /// <summary>
    /// Publishes a new track.
    /// </summary>
    /// <param name="spredUserId">User id.</param>
    /// <param name="trackDto">The track metadata.</param>
    /// <returns>The unique identifier of the published track.</returns>
    [Post("/internal/track/{spredUserId}")]
    public Task<ApiResponse<JsonElement>> AddTrack(string spredUserId, [Body] TrackDtoWithPlatformIds trackDto);
}