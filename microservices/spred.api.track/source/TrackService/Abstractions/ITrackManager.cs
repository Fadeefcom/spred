using TrackService.Models.Entities;

namespace TrackService.Abstractions;

/// <summary>
/// Interface for managing track metadata items.
/// </summary>
public interface ITrackManager
{
    /// <summary>
    /// Retrieves a collection of track metadata based on query parameters and user ID.
    /// </summary>
    /// <param name="queryParams">The query parameters for filtering the tracks.</param>
    /// <param name="spredUserId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="bucket">The bucket segment of the partition key (default is "00").</param>
    /// <returns>A collection of track metadata.</returns>
    Task<IEnumerable<TrackMetadata>> GetAsync(Dictionary<string, string> queryParams, Guid spredUserId, CancellationToken cancellationToken, string bucket = "00");

    /// <summary>
    /// Retrieves the total count of track metadata based on query parameters and user ID.
    /// </summary>
    /// <param name="queryParams">The query parameters for filtering the tracks.</param>
    /// <param name="spredUserId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="bucket">The bucket segment of the partition key (default is "00").</param>
    /// <returns>The total count of track metadata.</returns>
    Task<int> GetTotalAsync(Dictionary<string, string> queryParams, Guid spredUserId, CancellationToken cancellationToken, string bucket = "00");

    /// <summary>
    /// Retrieves a track metadata item by its ID and user ID.
    /// </summary>
    /// <param name="id">The unique identifier of the track.</param>
    /// <param name = "spredUserId" > The unique identifier of the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="bucket">The bucket segment of the partition key (default is "00").</param>
    /// <returns>The track metadata item.</returns>
    Task<TrackMetadata?> GetByIdAsync(Guid id, Guid spredUserId, CancellationToken cancellationToken, string bucket = "00");

    /// <summary>
    /// Adds a new track metadata item.
    /// </summary>
    /// <param name="userTrack"> track metadata to add.</param>
    /// <param name="spredUserId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unique identifier of the added track metadata item.</returns>
    Task<Guid> AddAsync(TrackMetadata userTrack, Guid spredUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a track metadata item by its ID and user ID.
    /// </summary>
    /// <param name="id">The unique identifier of the track.</param>
    /// <param name="spredUserId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="bucket">The bucket segment of the partition key (default is "00").</param>
    /// <returns>The result of the delete operation.</returns>
    Task<bool> DeleteAsync(Guid id, Guid spredUserId, CancellationToken cancellationToken, string bucket = "00");
    
    /// <summary>
    /// Updates a track metadata item.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<bool> UpdateAsync(TrackMetadata entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a track metadata item exists by its primary ID.
    /// </summary>
    /// <param name="primaryId">The primary identifier of the track.</param>
    /// <param name="spredUserId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the track metadata item exists, otherwise false.</returns>
    Task<Guid?> IfExistsByPrimaryId(string primaryId, Guid spredUserId, CancellationToken cancellationToken);
}
