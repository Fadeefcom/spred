using AggregatorService.Models.Dto;
using Refit;
using Spred.Bus.DTOs;

namespace AggregatorService.Abstractions;

/// <summary>
/// Defines the API for interacting with playlist services.
/// </summary>
public interface IPlaylistServiceApi
{
    /// <summary>
    /// Adds a new playlist to the service.
    /// </summary>
    /// <param name="bearerToken">The authorization token for the request.</param>
    /// <param name="playlistDto">The metadata of the playlist to be added.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the unique identifier of the created playlist.</returns>
    [Post("/internal/playlist")]
    public Task<ApiResponse<Guid>> AddPlaylist([Header("Authorization")] string bearerToken, MetadataDto playlistDto);
}
