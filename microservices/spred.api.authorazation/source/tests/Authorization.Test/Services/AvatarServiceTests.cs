using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Authorization.Abstractions;
using Authorization.Services;
using CloudinaryDotNet.Actions;
using Moq;

namespace Authorization.Test.Services;

public class AvatarServiceTests
{
    private readonly Mock<ICloudinaryWrapper> _cloudinaryMock;
    private readonly AvatarService _service;

    public AvatarServiceTests()
    {
        _cloudinaryMock = new Mock<ICloudinaryWrapper>();
        _service = new AvatarService(_cloudinaryMock.Object);
    }

    [Fact]
    public async Task SaveAvatarAsync_ShouldReturnUrl_WhenUploadSuccessful()
    {
        var userId = "user123";
        var stream = new MemoryStream(new byte[] {1, 2, 3});

        _cloudinaryMock
            .Setup(c => c.UploadAsync(It.IsAny<ImageUploadParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageUploadResult
            {
                StatusCode = HttpStatusCode.OK,
                SecureUrl = new Uri("https://res.cloudinary.com/test/image/upload/v1/avatars/user123/test.png")
            });

        var result = await _service.SaveAvatarAsync(userId, stream, "image/png", CancellationToken.None);

        Assert.Equal("https://res.cloudinary.com/test/image/upload/v1/avatars/user123/test.png", result);
    }

    [Fact]
    public async Task SaveAvatarAsync_ShouldThrow_WhenUploadFails()
    {
        _cloudinaryMock
            .Setup(c => c.UploadAsync(It.IsAny<ImageUploadParams>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageUploadResult { StatusCode = HttpStatusCode.BadRequest });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SaveAvatarAsync("user123", new MemoryStream(), "image/png", CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAvatarAsync_ShouldCallDestroy_WhenUrlValid()
    {
        var url = "https://res.cloudinary.com/test/image/upload/v1/avatars/user123/abc.png";

        _cloudinaryMock
            .Setup(c => c.DestroyAsync(It.IsAny<DeletionParams>(), CancellationToken.None))
            .ReturnsAsync(new DeletionResult { Result = "ok" });

        await _service.DeleteAvatarAsync("user123", url, CancellationToken.None);

        _cloudinaryMock.Verify(c =>
            c.DestroyAsync(It.Is<DeletionParams>(p => p.PublicId.Contains("avatars/user123/abc")), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAvatarAsync_ShouldNotCallDestroy_WhenUrlInvalid()
    {
        var url = "not-a-valid-url";

        await _service.DeleteAvatarAsync("user123", url, CancellationToken.None);

        _cloudinaryMock.Verify(c => c.DestroyAsync(It.IsAny<DeletionParams>(), CancellationToken.None), Times.Never);
    }
}