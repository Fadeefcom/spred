namespace TrackService.Abstractions;

/// <summary>
/// Provides methods for managing blob storage operations.
/// </summary>
public interface IBlobContainerProvider
{
    /// <summary>
    /// Uploads a file to the blob container.
    /// </summary>
    /// <param name="stream">The stream of the file to upload.</param>
    /// <param name="trackId">The unique identifier of the track.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the file name and URL.</returns>
    public Task<(string, string)> UploadFile(Stream stream, Guid trackId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a file from the blob container if it exists.
    /// </summary>
    /// <param name="trackName">The unique identifier of the track.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteIfExists(Guid trackName, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a file exists in the blob container.
    /// </summary>
    /// <param name="trackName">The unique identifier of the track.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the file exists.</returns>
    public Task<bool> CheckIfExists(Guid trackName, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a file from the blob container.
    /// </summary>
    /// <param name="trackName">The unique identifier of the track.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file stream.</returns>
    public Task<Stream> GetFile(Guid trackName, CancellationToken cancellationToken);
}
