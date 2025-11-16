using System.Text.Json;
using Refit;

namespace SubmissionService.Abstractions;

/// <summary>
/// Defines a contract for interacting with the Catalog Service API.
/// Provides operations for retrieving playlist information by identifier.
/// </summary>
public interface ICatalogService
{
    /// <summary>
    /// Asynchronously retrieves a playlist by its identifier for a given user.
    /// </summary>
    /// <param name="trackId">The identifier of the playlist (catalog item) to retrieve.</param>
    /// <param name="spredUserId">The identifier of the Spred user making the request.</param>
    /// <param name="cancellationToken">A token to observe cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the API response wrapping the JSON playlist data.
    /// </returns>
    [Get("/internal/playlist/{spredUserId}/{trackId}")]
    public Task<IApiResponse<JsonElement>> GetPlaylistByIdAsync(string trackId, string spredUserId, CancellationToken cancellationToken = default);
}