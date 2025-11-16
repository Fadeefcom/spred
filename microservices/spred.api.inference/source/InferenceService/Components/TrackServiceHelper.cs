using Azure.Storage.Blobs;
using InferenceService.Abstractions;
using InferenceService.Configuration;
using Microsoft.Extensions.Options;

namespace InferenceService.Components;

/// <inheritdoc />
public class TrackServiceHelper : ITrackServiceHelper
{
    private readonly ITrackServiceApi _trackServiceApi;
    private readonly BlobContainerClient _blobContainerClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackServiceHelper"/> class.
    /// </summary>
    /// <param name="trackServiceApi">The track API.</param>
    /// <param name="blobOptions">The options containing the Blob storage configuration.</param>
    public TrackServiceHelper(ITrackServiceApi trackServiceApi, IOptions<BlobOptions> blobOptions)
    {
        _trackServiceApi = trackServiceApi;
        var blobServiceClient1 = new BlobServiceClient(blobOptions.Value.BlobConnectString);
        _blobContainerClient = blobServiceClient1.GetBlobContainerClient(blobOptions.Value.ContainerName);
    }

    /// <inheritdoc />
    public async Task<Stream?> GetTrackStream(Guid spredUserId, Guid trackId)
    {
        var result = await _trackServiceApi.GetTrackStreamById(spredUserId.ToString(), trackId.ToString());
        if (result.IsSuccessful)
            return result.Content;
        return null;
    }

    /// <inheritdoc />
    public async Task<Stream> GetTrackStreamBlob(Guid trackId, CancellationToken cancellationToken)
    {
        var blobClient = _blobContainerClient.GetBlobClient(trackId.ToString());
        var downloadResponse = await blobClient.DownloadAsync(cancellationToken);
        return downloadResponse.Value.Content;
    }
}
