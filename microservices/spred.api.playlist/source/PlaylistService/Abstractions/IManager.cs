using PlaylistService.Models;
using PlaylistService.Models.Entities;

namespace PlaylistService.Abstractions;

/// <summary>
/// Interface for managing playlist operations.
/// </summary>
public interface IManager
{
    /// <summary>
    /// Adds a new playlist asynchronously.
    /// </summary>
    /// <param name="entity">The playlist metadata entity to add.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
    public Task<bool> AddAsync(CatalogMetadata entity, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a playlist asynchronously.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist to delete.</param>
    /// <param name="spredUserId">The ID of the user who owns the playlist.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <param name="bucket">The bucket segment of the partition key (default is "00").</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
    public Task<bool> DeleteAsync(Guid playlistId, Guid spredUserId, CancellationToken cancellationToken, string bucket = "00");

    /// <summary>
    /// Updates an existing playlist asynchronously.
    /// </summary>
    /// <param name="entity">The playlist metadata entity to update.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
    public Task<bool> UpdateAsync(CatalogMetadata entity, CancellationToken cancellationToken);

    /// <summary>
    /// Finds a playlist by its ID asynchronously.
    /// </summary>
    /// <param name="id">The ID of the playlist to find.</param>
    /// <param name="spredUserId">The ID of the user who owns the playlists.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <param name="bucket">The bucket segment of the partition key (default is "00").</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the playlist metadata entity if found, otherwise null.</returns>
    public Task<CatalogMetadata?> FindByIdAsync(Guid id, Guid spredUserId, CancellationToken cancellationToken, string bucket = "00");

    /// <summary>
    /// Gets a list of playlists based on query parameters asynchronously.
    /// </summary>
    /// <param name="queryParams">The query parameters to filter playlists.</param>
    /// <param name="type">Metadata type.</param>
    /// <param name="spredUserId">The ID of the user who owns the playlists.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <param name="bucket">The bucket segment of the partition key (default is "00").</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task<IEnumerable<CatalogMetadata>> GetAsync(Dictionary<string, string> queryParams, string type, Guid spredUserId, CancellationToken cancellationToken, string bucket = "00");

    /// <summary>
    /// Check if track exists by primaryId
    /// </summary>
    /// <param name="primaryId"></param>
    /// <param name="spredUserId">The ID of the user who owns the playlists.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns></returns>
    public Task<Guid?> ExistsByPrimaryIdAsync(PrimaryId primaryId, Guid spredUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Calculates the follower difference for the specified playlist over the last 30 days.
    /// </summary>
    /// <param name="playlistId">The ID of the playlist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The follower count difference (newest - oldest) for the last 30 days.</returns>
    public Task<int> GetStatisticDifference(Guid playlistId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a list of <see cref="CatalogMetadata"/> documents of the specified type
    /// by matching their IDs and owner partition keys.
    /// </summary>
    /// <param name="ownerMetadataIds">
    /// A dictionary where the key is the owner ID (used as the partition key),
    /// and the value is a list of metadata IDs belonging to that owner.
    /// </param>
    /// <param name="type">
    /// The expected type of metadata to filter by (e.g. "playlist").
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a list of
    /// <see cref="CatalogMetadata"/> documents that match the given IDs and type.
    /// </returns>
    public Task<List<CatalogMetadata>> GetPlaylistsByIdsAsync(Dictionary<Guid, List<Guid>> ownerMetadataIds,
        string type,
        CancellationToken cancellationToken);
}
