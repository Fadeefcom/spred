using TrackService.Abstractions;

namespace TrackService.Components.Services;

/// <summary>
/// Service for uploading, deleting, checking existence, and retrieving tracks from blob storage.
/// </summary>
public class UploadTrackService : IUploadTrackService
{
    private IBlobContainerProvider BlobContainer { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadTrackService"/> class.
    /// </summary>
    /// <param name="blobContainer">The blob container to interact with.</param>
    public UploadTrackService(IBlobContainerProvider blobContainer)
    {
        BlobContainer = blobContainer;
    }

    /// <summary>
    /// Uploads a track to the blob storage.
    /// </summary>
    /// <param name="file">The file stream to upload.</param>
    /// <param name="trackId">The unique identifier for the track.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>A tuple containing the file path and the trusted file name.</returns>
    public async Task<(string, string)> UploadTrackAsync(Stream file, Guid trackId, CancellationToken token) =>
        await BlobContainer.UploadFile(file, trackId, token);

    /// <summary>
    /// Deletes a file from the blob storage if it exists.
    /// </summary>
    /// <param name="fileName">The unique identifier for the track.</param>
    /// <param name="token">The cancellation token.</param>
    public async Task DeleteFileIfExists(Guid fileName, CancellationToken token) =>
        await BlobContainer.DeleteIfExists(fileName, token);

    /// <summary>
    /// Checks if a file exists in the blob storage.
    /// </summary>
    /// <param name="fileName">The unique identifier for the track.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>True if the file exists, otherwise false.</returns>
    public async Task<bool> CheckIfExists(Guid fileName, CancellationToken token) =>
        await BlobContainer.CheckIfExists(fileName, token);

    /// <summary>
    /// Retrieves a file from the blob storage as a stream.
    /// </summary>
    /// <param name="fileName">The unique identifier for the track.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file stream.</returns>
    public async Task<Stream> GetStream(Guid fileName, CancellationToken cancellationToken) =>
        await BlobContainer.GetFile(fileName, cancellationToken);
}