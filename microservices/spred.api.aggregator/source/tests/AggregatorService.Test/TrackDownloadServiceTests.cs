using AggregatorService.Components;
using AggregatorService.Test.Helpers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using Repository.Abstractions.Models;

namespace AggregatorService.Test;

public class TrackDownloadServiceTests
{
    [Fact]
    public void GetTrackFromYoutubeCommand_Should_Return_Track_After_Populate()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dbOptions = Options.Create(new DbConnectionOptions
        {
            AccountEndPoint = "https://fake-account.documents.azure.com:443/",
            AccountKey = "testaccountkey==",
            DatabaseName = "TestDb",
            EnsureCreated = false
        });

        var mockTrack = new JObject
        {
            ["id"] = Guid.NewGuid(),
            ["PrimaryId"] = "spotify:track:abc",
            ["Title"] = "Test Title",
            ["Artists"] = new JArray(new JObject { ["Name"] = "Test Artist" }),
            ["Status"] = 0
        };

        var mockFeedIterator = new Mock<FeedIterator<JObject>>();
        mockFeedIterator.SetupSequence(m => m.HasMoreResults)
            .Returns(true)
            .Returns(false);

        var fakeResponse = new FakeFeedResponse<JObject>([mockTrack]);
        mockFeedIterator.Setup(m => m.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => fakeResponse);

        var mockContainer = new Mock<Container>();
        mockContainer.Setup(m => m.GetItemQueryIterator<JObject>(
                It.IsAny<QueryDefinition>(),
                null,
                It.IsAny<QueryRequestOptions>()))
            .Returns(mockFeedIterator.Object);

        var mockCosmosClient = new Mock<CosmosClient>();
        mockCosmosClient.Setup(x => x.GetContainer("TestDb", "TrackMetadata"))
                        .Returns(mockContainer.Object);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(x => x.GetService(typeof(CosmosClient)))
                           .Returns(mockCosmosClient.Object);

        var service = new TrackDownloadService(loggerFactory, dbOptions, mockCosmosClient.Object);

        typeof(TrackDownloadService)
            .GetField("_trackContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(service, mockContainer.Object);

        // Act
        service.GetTrackFromYoutubeCommand();
        var result = service.GetTrackFromYoutubeCommand();

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("spotify:track", result.PrimaryId);
    }
    
    [Fact]
    public void GetTrackFromYoutubeCommand_Should_Return_Null_When_Stack_Empty()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dbOptions = Options.Create(new DbConnectionOptions
        {
            AccountEndPoint = "https://fake-account.documents.azure.com:443/",
            AccountKey = "test",
            DatabaseName = "TestDb",
            EnsureCreated = false
        });

        var mockContainer = new Mock<Container>();
        var mockClient = new Mock<CosmosClient>();
        mockClient.Setup(x => x.GetContainer("TestDb", "TrackMetadata_v2")).Returns(mockContainer.Object);

        var service = new TrackDownloadService(loggerFactory, dbOptions, mockClient.Object);

        // Act
        var result = service.GetTrackFromYoutubeCommand();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task PopulateTracks_Should_Log_Exception_When_Failure_Occurs()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TrackDownloadService>>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

        var dbOptions = Options.Create(new DbConnectionOptions
        {
            AccountEndPoint = "https://fake.documents.azure.com",
            AccountKey = "test==",
            DatabaseName = "TestDb",
            EnsureCreated = false
        });

        var mockContainer = new Mock<Container>();
        mockContainer.Setup(m => m.GetItemQueryIterator<JObject>(
            It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>()))
            .Throws(new System.Exception("Simulated failure"));

        var mockClient = new Mock<CosmosClient>();
        mockClient.Setup(x => x.GetContainer("TestDb", "TrackMetadata_v2"))
                  .Returns(mockContainer.Object);

        var _ = new TrackDownloadService(loggerFactory.Object, dbOptions, mockClient.Object);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                new EventId(5000, "LogSpredError"),
                It.Is<It.IsAnyType>((v, t) => true), // не проверяем state
                It.Is<System.Exception>(ex => ex.Message.Contains("Simulated failure")),
                It.IsAny<Func<It.IsAnyType, System.Exception, string>>()!),
            Times.Once);
    }

    [Fact]
    public void Dispose_Should_Dispose_Semaphore()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var dbOptions = Options.Create(new DbConnectionOptions
        {
            AccountEndPoint = "https://fake.documents.azure.com",
            AccountKey = "test",
            DatabaseName = "TestDb",
            EnsureCreated = false
        });

        var mockContainer = new Mock<Container>();
        var mockClient = new Mock<CosmosClient>();
        mockClient.Setup(x => x.GetContainer("TestDb", "TrackMetadata_v2"))
                  .Returns(mockContainer.Object);

        var service = new TrackDownloadService(loggerFactory, dbOptions, mockClient.Object);

        var field = typeof(TrackDownloadService).GetField("_populateLock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var semaphore = (SemaphoreSlim)field.GetValue(service)!;

        // Act
        service.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => semaphore.Wait(0));
    }
}