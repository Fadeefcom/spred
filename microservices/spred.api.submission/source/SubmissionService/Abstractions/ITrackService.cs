using System.Text.Json;
using Refit;

namespace SubmissionService.Abstractions;

/// <summary>
/// Defines a contract for interacting with the Track Service API.
/// Provides operations for retrieving track information by identifier.
/// </summary>
public interface ITrackService
{
    /// <summary>
    /// Asynchronously retrieves a track by its identifier for a given user.
    /// </summary>
    /// <param name="trackId">The identifier of the track to retrieve.</param>
    /// <param name="spredUserId">The identifier of the Spred user making the request.</param>
    /// <param name="cancellationToken">A token to observe cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. 
    /// The task result contains the API response wrapping the JSON track data.
    /// </returns>
    [Get("/internal/track/{spredUserId}/{trackId}")]
    public Task<IApiResponse<JsonElement>> GetTrackByIdAsync(string trackId, string spredUserId, CancellationToken cancellationToken = default);
}