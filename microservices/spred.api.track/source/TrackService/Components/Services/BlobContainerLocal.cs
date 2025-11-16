using Microsoft.Extensions.Options;
using TrackService.Abstractions;
using TrackService.Configuration;

namespace TrackService.Components.Services;

/// <summary>
/// Provides local blob storage operations.
/// </summary>
public class BlobContainerLocal : IBlobContainerProvider
{
    private readonly ILogger<BlobContainerLocal> _logger;
    private readonly string _folderPath;
    private readonly string _containerName;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlobContainerLocal"/> class.
    /// </summary>
    /// <param name="blobOptions">The blob options.</param>
    /// <param name="factory">The logger factory.</param>
    public BlobContainerLocal(IOptions<BlobOptions> blobOptions, ILoggerFactory factory)
    {
        _logger = factory.CreateLogger<BlobContainerLocal>();
        _containerName = blobOptions.Value.ContainerName;
        _folderPath = Path.Combine(Environment.CurrentDirectory, blobOptions.Value.ContainerName);
        Directory.CreateDirectory(_folderPath);
    }

    /// <inheritdoc/>
    public Task<bool> CheckIfExists(Guid trackName, CancellationToken cancellationToken)
    {
        var result = File.Exists(Path.Combine(_folderPath, trackName.ToString()));
        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task DeleteIfExists(Guid trackName, CancellationToken cancellationToken)
    {
        if (CheckIfExists(trackName, cancellationToken).Result)
        {
            File.Delete(Path.Combine(_folderPath, trackName.ToString()));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<Stream> GetFile(Guid trackName, CancellationToken cancellationToken)
    {
        var readStream = File.OpenRead(Path.Combine(_folderPath, trackName.ToString()));
        return Task.FromResult<Stream>(readStream);
    }

    /// <inheritdoc/>
    public async Task<(string, string)> UploadFile(Stream stream, Guid trackId, CancellationToken cancellationToken)
    {
        await using var writeStream = File.Create(Path.Combine(_folderPath, trackId.ToString()));
        await stream.CopyToAsync(writeStream, cancellationToken);
        return (_containerName, trackId.ToString());
    }
}
