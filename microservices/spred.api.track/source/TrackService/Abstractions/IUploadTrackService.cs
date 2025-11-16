
namespace TrackService.Abstractions;


/// <summary>
/// Provides methods for uploading, deleting, checking existence, and retrieving track files.
/// </summary>
public interface IUploadTrackService
{
    /// <summary>
    /// Uploads a track file asynchronously.
    /// </summary>
    /// <param name="file">The stream of the file to be uploaded.</param>
    /// <param name="trackId">The unique identifier of the track.</param>
    /// <param name="token">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the path and track name.</returns>
    public Task<(string, string)> UploadTrackAsync(Stream file, Guid trackId, CancellationToken token = default);

    /// <summary>
    /// Deletes a file if it exists.
    /// </summary>
    /// <param name="fileName">The unique identifier of the file to be deleted.</param>
    /// <param name="token">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteFileIfExists(Guid fileName, CancellationToken token = default);

    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    /// <param name="fileName">The unique identifier of the file to check.</param>
    /// <param name="token">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the file exists.</returns>
    public Task<bool> CheckIfExists(Guid fileName, CancellationToken token = default);

    /// <summary>
    /// Gets a stream of a file from the blob container.
    /// </summary>
    /// <param name="fileName">The unique identifier of the file to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the stream of the file.</returns>
    public Task<Stream> GetStream(Guid fileName, CancellationToken cancellationToken = default);
}
