using System.Security.Claims;
using AggregatorService.Abstractions;
using AggregatorService.Components;
using Extensions.Interfaces;
using Extensions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Refit;

namespace AggregatorService.Test;

public class TrackSenderServiceTests
{
    [Fact]
    public async Task PushTrack_Should_Call_Api_And_Delete_Temp_File()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        var content = "test content";
        var fileName = "track.mp3";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.ContentType).Returns("audio/mpeg");
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
            .Returns<Stream, CancellationToken>((target, _) => stream.CopyToAsync(target));

        var apiMock = new Mock<ITrackServiceApi>();
        apiMock.Setup(api => api.AddAudioTrack(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<StreamPart>()))
               .Returns(Task.CompletedTask);

        var tokenProviderMock = new Mock<IGetToken>();
        tokenProviderMock.Setup(t => t.GetInternalTokenAsync(It.IsAny<Claim[]>()))
                         .ReturnsAsync("mock-token");

        var options = Options.Create(new ServicesOuterOptions
        {
            AggregatorService = "http://localhost",
            TrackService = "http://localhost",
            AuthorizationService = "http://localhost",
            InferenceService = "http://localhost",
            PlaylistService = "http://localhost",
            UiEndpoint = "http://localhost",
            VectorService = "http://localhost",
            SubscriptionService = "http://localhost"
        });

        var service = new TrackSenderService(options);

        // inject mock manually (to replace Refit-generated default)
        typeof(TrackSenderService)
            .GetField("_trackServiceApi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(service, apiMock.Object);

        var trackId = Guid.NewGuid();

        // Act
        await service.PushTrack(fileMock.Object, trackId);
        await Task.Delay(500); // let background task finish

        // Assert
        apiMock.Verify(x =>
            x.AddAudioTrack(
                It.IsAny<string>(),
                It.Is<string>(id => id == trackId.ToString()),
                It.IsAny<StreamPart>()),
            Times.Once);
    }

    [Fact]
    public async Task UnsuccessfulResult_Should_Call_TrackServiceApi()
    {
        // Arrange
        var apiMock = new Mock<ITrackServiceApi>();
        apiMock.Setup(x => x.UnsuccessfulResult(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var tokenProviderMock = new Mock<IGetToken>();
        tokenProviderMock.Setup(t => t.GetInternalTokenAsync(It.IsAny<Claim[]>()))
            .ReturnsAsync("mock-token");

        var options = Options.Create(new ServicesOuterOptions
        {
            AggregatorService = "http://localhost",
            TrackService = "http://localhost",
            AuthorizationService = "http://localhost",
            InferenceService = "http://localhost",
            PlaylistService = "http://localhost",
            UiEndpoint = "http://localhost",
            VectorService = "http://localhost",
            SubscriptionService = "http://localhost"
        });

        var service = new TrackSenderService(options);

        typeof(TrackSenderService)
            .GetField("_trackServiceApi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(service, apiMock.Object);

        var trackId = Guid.NewGuid();

        // Act
        await service.UnsuccessfulResult(trackId);
        await Task.Delay(300); // let background task finish

        // Assert
        apiMock.Verify(x =>
            x.UnsuccessfulResult(
                It.IsAny<string>(),
                It.Is<string>(id => id == trackId.ToString())),
            Times.Once);
    }
}