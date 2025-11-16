namespace InferenceService.Abstractions;

/// <summary>
/// Helper class for interacting with the track service API.
/// </summary>
public interface ITrackServiceHelper
{
    /// <summary>
    /// Retrieves the audio track stream by its identifier from the track service API.
    /// </summary>
    /// <param name="spredUserId">The Spred user ID for authentication.</param>
    /// <param name="trackId">The identifier of the track to retrieve.</param>
    /// <returns>A stream containing the audio track.</returns>
    public Task<Stream?> GetTrackStream(Guid spredUserId, Guid trackId);

    /// <summary>
    /// Retrieves the audio track stream by its identifier from Blob storage.
    /// </summary>
    /// <param name="trackId">The identifier of the track to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A stream containing the audio track.</returns>
    public Task<Stream> GetTrackStreamBlob(Guid trackId, CancellationToken cancellationToken);
}