using System.Reflection;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using InferenceService.Abstractions;
using InferenceService.Components;
using InferenceService.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Refit;

namespace InferenceService.Test;

public class TrackServiceHelperTests
{
    private readonly Mock<ITrackServiceApi> _apiMock = new();
    private readonly Mock<BlobContainerClient> _containerMock = new();
    private readonly TrackServiceHelper _helper;

    public TrackServiceHelperTests()
    {
        var blobOptions = Options.Create(new BlobOptions
        {
            BlobConnectString = "UseDevelopmentStorage=true",
            ContainerName = "mock-container"
        });

        var blobServiceMock = new Mock<BlobServiceClient>(MockBehavior.Strict, blobOptions.Value.BlobConnectString);
        blobServiceMock
            .Setup(x => x.GetBlobContainerClient(blobOptions.Value.ContainerName))
            .Returns(_containerMock.Object);

        _helper = new TrackServiceHelper(_apiMock.Object, blobOptions);
    }

    [Fact]
    public async Task GetTrackStream_ShouldReturnContent_WhenSuccessful()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("dummy"));
        var response = new ApiResponse<Stream>(new HttpResponseMessage(System.Net.HttpStatusCode.OK), stream, new RefitSettings());

        _apiMock.Setup(x => x.GetTrackStreamById(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        var result = await _helper.GetTrackStream(Guid.NewGuid(), Guid.NewGuid());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTrackStream_ShouldReturnNull_WhenNotSuccessful()
    {
        var response = new ApiResponse<Stream>(new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest), null!, new RefitSettings());

        _apiMock.Setup(x => x.GetTrackStreamById(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        var result = await _helper.GetTrackStream(Guid.NewGuid(), Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTrackStreamBlob_ShouldReturnStream_WithReflectionMock()
    {
        // Arrange: мок контент
        var expectedContent = "blob content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(expectedContent));

        var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: stream);
        var response = Response.FromValue(blobDownloadInfo, Mock.Of<Response>());

        // Мокаем BlobClient
        var blobClientMock = new Mock<BlobClient>();
        blobClientMock
            .Setup(x => x.DownloadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Мокаем BlobContainerClient
        var containerMock = new Mock<BlobContainerClient>();
        containerMock
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClientMock.Object);

        // Создаем валидные ITrackServiceApi и IOptions<BlobOptions>
        var apiMock = new Mock<ITrackServiceApi>();
        var options = Options.Create(new BlobOptions
        {
            BlobConnectString = "UseDevelopmentStorage=true",
            ContainerName = "fake"
        });

        // Инициализируем реальный объект (с настоящим BlobServiceClient внутри)
        var helper = new TrackServiceHelper(_apiMock.Object, options);

        // Подменяем приватное поле _blobContainerClient через рефлексию
        var field = typeof(TrackServiceHelper)
            .GetField("_blobContainerClient", BindingFlags.NonPublic | BindingFlags.Instance)!;

        field.SetValue(helper, containerMock.Object);

        // Act
        var resultStream = await helper.GetTrackStreamBlob(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.NotNull(resultStream);
        using var reader = new StreamReader(resultStream);
        var actualContent = await reader.ReadToEndAsync();
        Assert.Equal(expectedContent, actualContent);
    }
}