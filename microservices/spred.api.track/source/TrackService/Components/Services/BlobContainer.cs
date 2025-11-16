using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Exception;
using Exception.Exceptions;
using Extensions.Extensions;
using Microsoft.Extensions.Options;
using TrackService.Abstractions;
using TrackService.Configuration;

namespace TrackService.Components.Services;

/// <summary>
/// Provides methods to interact with Azure Blob Storage, including uploading, deleting, checking existence, and retrieving files.
/// </summary>
public class BlobContainer : IBlobContainerProvider
{
    private readonly ILogger<BlobContainer> _logger;
    private readonly BlobContainerClient _blobContainerClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobContainer"/> class.
    /// </summary>
    /// <param name="blobOptions">The options for configuring the blob storage.</param>
    /// <param name="factory">The logger factory to create loggers.</param>
    public BlobContainer(IOptions<BlobOptions> blobOptions, ILoggerFactory factory)
    {
        _logger = factory.CreateLogger<BlobContainer>();
        var blobServiceClient = new BlobServiceClient(blobOptions.Value.BlobConnectString);
        _blobContainerClient = blobServiceClient.GetBlobContainerClient(blobOptions.Value.ContainerName);
    }

    /// <summary>
    /// Uploads a file to the blob storage.
    /// </summary>
    /// <param name="stream">The file stream to upload.</param>
    /// <param name="trackId">The unique identifier for the track.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tuple containing the file path and the trusted file name.</returns>
    /// <exception cref="Exception">Thrown when the upload fails.</exception>
    public async Task<(string, string)> UploadFile(Stream stream, Guid trackId, CancellationToken cancellationToken)
    {
        try
        {
            var filePath = Path.Combine(_blobContainerClient.Name, trackId.ToString());
            var trustedFileName = trackId.ToString();

            var result = await _blobContainerClient.UploadBlobAsync(trustedFileName, stream, cancellationToken);

            _logger.LogSpredInformation(nameof(BlobContainer), $"blobs return Object: {result.GetRawResponse()}");

            return (filePath, trustedFileName);
        }
        catch (System.Exception ex)
        {
            throw ExceptionHandler.ConvertException(ex, (int)ErrorCode.InsufficientStorage, "Blob upload file failed.");
        }
    }

    /// <summary>
    /// Deletes a file from the blob storage if it exists.
    /// </summary>
    /// <param name="trackName">The unique identifier for the track.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DeleteIfExists(Guid trackName, CancellationToken cancellationToken)
    {
        var res = await _blobContainerClient.DeleteBlobIfExistsAsync(trackName.ToString(), cancellationToken: cancellationToken);

        _logger.LogSpredAudit(nameof(BlobContainer), $"Delete {trackName.ToString()} with result: {res}");
    }

    /// <summary>
    /// Checks if a file exists in the blob storage.
    /// </summary>
    /// <param name="trackName">The unique identifier for the track.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the file exists, otherwise false.</returns>
    public async Task<bool> CheckIfExists(Guid trackName, CancellationToken cancellationToken)
    {
        await foreach (var blobItem in _blobContainerClient.GetBlobsAsync(
            BlobTraits.Metadata,
            BlobStates.None,
            prefix: trackName.ToString(),
            cancellationToken: cancellationToken))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Retrieves a file from the blob storage as a stream.
    /// </summary>
    /// <param name="trackName">The unique identifier for the track.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file stream.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found.</exception>
    /// <exception cref="BaseException">Thrown when the retrieval fails.</exception>
    public async Task<Stream> GetFile(Guid trackName, CancellationToken cancellationToken)
    {
        try
        {
            var blobClient = _blobContainerClient.GetBlobClient(trackName.ToString());

            if (await blobClient.ExistsAsync(cancellationToken))
            {
                var downloadResponse = await blobClient.DownloadAsync(cancellationToken);
                return downloadResponse.Value.Content;
            }
            else
            {
                throw new FileNotFoundException("Blob not found.");
            }
        }
        catch (System.Exception ex)
        {
            throw ExceptionHandler.ConvertException(ex, (int)ErrorCode.InsufficientStorage, "Blob file retrieval failed.");
        }
    }
}