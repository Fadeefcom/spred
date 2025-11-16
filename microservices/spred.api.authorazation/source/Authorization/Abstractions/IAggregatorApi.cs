using Authorization.Models.Dto;
using Refit;

namespace Authorization.Abstractions;

/// <summary>
/// Defines the API contract for interacting with the Aggregator service.
/// </summary>
public interface IAggregatorApi
{
    /// <summary>
    /// Queues user playlists for processing in the Aggregator service.
    /// </summary>
    /// <param name="bearerToken">The authorization token to authenticate the request.</param>
    /// <param name="request">The request payload containing playlist details to be queued.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Post("/aggregator/playlist/queue")]
    public Task<ApiResponse<object>> QueueUserPlaylists([Header("Authorization")] string bearerToken, [Body] QueueUserPlaylistsRequest request);
}
