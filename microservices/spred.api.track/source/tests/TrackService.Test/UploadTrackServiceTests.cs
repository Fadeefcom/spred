using System.Text;
using Moq;
using TrackService.Abstractions;
using TrackService.Components.Services;

namespace TrackService.Test;

public class UploadTrackServiceTests
{
    private readonly Mock<IBlobContainerProvider> _blobMock = new();
    private readonly UploadTrackService _service;

    public UploadTrackServiceTests()
    {
        _service = new UploadTrackService(_blobMock.Object);
    }

    [Fact]
    public async Task UploadTrackAsync_ShouldReturnTuple()
    {
        // Arrange
        var trackId = Guid.NewGuid();
        var content = new MemoryStream(Encoding.UTF8.GetBytes("audio"));
        var expected = ("container", trackId.ToString());

        _blobMock
            .Setup(x => x.UploadFile(content, trackId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _service.UploadTrackAsync(content, trackId, CancellationToken.None);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task DeleteFileIfExists_ShouldInvokeBlobDelete()
    {
        var id = Guid.NewGuid();

        _blobMock
            .Setup(x => x.DeleteIfExists(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _service.DeleteFileIfExists(id, CancellationToken.None);

        _blobMock.Verify();
    }

    [Fact]
    public async Task CheckIfExists_ShouldReturnTrue_WhenFileExists()
    {
        var id = Guid.NewGuid();
        _blobMock
            .Setup(x => x.CheckIfExists(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exists = await _service.CheckIfExists(id, CancellationToken.None);

        Assert.True(exists);
    }

    [Fact]
    public async Task CheckIfExists_ShouldReturnFalse_WhenFileNotExists()
    {
        var id = Guid.NewGuid();
        _blobMock
            .Setup(x => x.CheckIfExists(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exists = await _service.CheckIfExists(id, CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task GetStream_ShouldReturnBlobStream()
    {
        var id = Guid.NewGuid();
        var expectedStream = new MemoryStream(new byte[] { 1, 2, 3 });

        _blobMock
            .Setup(x => x.GetFile(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        var stream = await _service.GetStream(id, CancellationToken.None);

        Assert.NotNull(stream);
        Assert.Equal(expectedStream, stream);
    }
}