using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TrackService.Components.Services;
using TrackService.Configuration;
using TrackService.Test.Helpers;

namespace TrackService.Test;

public class BlobContainerTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();
    private readonly Mock<ILogger<BlobContainer>> _loggerMock = new();
    private readonly Mock<IOptions<BlobOptions>> _optionsMock = new();
    private readonly BlobOptions _blobOptions = new()
    {
        BlobConnectString = "UseDevelopmentStorage=true",
        ContainerName = "test-container"
    };

    private readonly Mock<BlobContainerClient> _blobContainerClientMock = new();
    private readonly Mock<BlobClient> _blobClientMock = new();

    private readonly BlobContainer _service;

    public BlobContainerTests()
    {
        _optionsMock.Setup(x => x.Value).Returns(_blobOptions);
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

        // Inject your own OnConfigure via shim here
        _service = new BlobContainer(_optionsMock.Object, _loggerFactoryMock.Object);
        
        typeof(BlobContainer)
            .GetField("_blobContainerClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            !.SetValue(_service, _blobContainerClientMock.Object);
        
        _blobContainerClientMock
            .Setup(x => x.Name)
            .Returns("test-container");
    }

    [Fact]
    public async Task UploadFile_ShouldUploadSuccessfully()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        _blobContainerClientMock
            .Setup(x => x.CreateIfNotExistsAsync(
                PublicAccessType.None,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue<BlobContainerInfo>(
                BlobsModelFactory.BlobContainerInfo( default, default),
                null!
            ));

        _blobContainerClientMock
            .Setup(x => x.UploadBlobAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue<BlobContentInfo>(null!, null!));

        // Act
        var (path, name) = await _service.UploadFile(stream, Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.NotNull(path);
        Assert.NotNull(name);
    }

    [Fact]
    public async Task DeleteIfExists_ShouldCallDelete()
    {
        // Arrange
        _blobContainerClientMock
            .Setup(x => x.CreateIfNotExistsAsync(
                PublicAccessType.None,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue<BlobContainerInfo>(
                BlobsModelFactory.BlobContainerInfo(default, default),
                null!
            ));

        _blobContainerClientMock
            .Setup(x => x.DeleteBlobIfExistsAsync(It.IsAny<string>(), DeleteSnapshotsOption.None, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, null!));

        // Act
        await _service.DeleteIfExists(Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task CheckIfExists_ShouldReturnTrue_WhenBlobExists()
    {
        var blobItem = BlobsModelFactory.BlobItem(name: "some-track-id");

        _blobContainerClientMock
            .Setup(x => x.CreateIfNotExistsAsync(
                PublicAccessType.None,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue<BlobContainerInfo>(
                BlobsModelFactory.BlobContainerInfo(default, default),
                null!
            ));

        _blobContainerClientMock
            .Setup(x => x.GetBlobsAsync(
                BlobTraits.Metadata, 
                BlobStates.None, 
                It.IsAny<string>(), 
                It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncPageable<BlobItem>([blobItem]));

        // Act
        var exists = await _service.CheckIfExists(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task GetFile_ShouldReturnStream_WhenBlobExists()
    {
        var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream(new byte[] { 1, 2, 3 }));

        _blobContainerClientMock
            .Setup(x => x.CreateIfNotExistsAsync(
                PublicAccessType.None,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue<BlobContainerInfo>(
                BlobsModelFactory.BlobContainerInfo(default, default),
                null!
            ));

        _blobContainerClientMock
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(_blobClientMock.Object);

        _blobClientMock
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, null!));

        _blobClientMock
            .Setup(x => x.DownloadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(blobDownloadInfo, null!));

        // Act
        var stream = await _service.GetFile(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.NotNull(stream);
    }
}
