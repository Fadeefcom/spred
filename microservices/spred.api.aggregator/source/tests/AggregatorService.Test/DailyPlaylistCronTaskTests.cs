using AggregatorService.BackgroundTasks;
using AggregatorService.Test.Helpers;
using MassTransit;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Spred.Bus.Contracts;
using StackExchange.Redis;

namespace AggregatorService.Test;

public class DailyPlaylistCronTaskTests
{
    [Fact]
    public async Task Should_Publish_CatalogUpdateRequest_For_Valid_Playlist()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DailyPlaylistCronTask>>();
        var mockPublishEndpoint = new Mock<ISendEndpointProvider>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockCosmosClient = new Mock<CosmosClient>();
        var mockContainer = new Mock<Container>();
        var mockFeedIterator = new Mock<FeedIterator<JObject>>();
        var mockSendEndpoint = new Mock<ISendEndpoint>();
        var dbMock = new Mock<IDatabase>();
        
        mockSendEndpoint
            .Setup(x => x.Send(It.IsAny<CatalogEnrichmentRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockPublishEndpoint
            .Setup(x => x.GetSendEndpoint(It.Is<Uri>(uri => uri.ToString() == "exchange:catalog-enrichment-request")))
            .ReturnsAsync(mockSendEndpoint.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "DbConnectionOptions:DatabaseName", "TestDb" },
                { "DailyPlaylistCronTask:CronTime", "00:00:00" },
                { "DailyPlaylistCronTask:RunOnStart", "true" }
            }!)
            .Build();

        var fakePlaylist = JObject.FromObject(new
        {
            id = Guid.NewGuid(),
            PrimaryId = "spotify:track:70kA7psKo9FAoFjKPClLDP",
            Bucket = "00",
            ChartmetricsId = "123",
            Type = "playlistMetadata",
            UpdatedAt = DateTime.UtcNow.AddDays(-8),
            NeedUpdateStatInfo = "true",
            SpredUserId = Guid.Empty
        });
        
        var fakeResponse = new FakeFeedResponse<JObject>([fakePlaylist]);

        var readCount = 0;
        mockFeedIterator.Setup(m => m.HasMoreResults)
            .Returns(() => readCount < 10);

        mockFeedIterator.Setup(m => m.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                readCount++;
                return fakeResponse;
            });

        mockContainer.Setup(m => m.GetItemQueryIterator<JObject>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()))
            .Returns(mockFeedIterator.Object);

        mockCosmosClient.Setup(m => m.GetContainer("TestDb", "CatalogMetadata_v2")).Returns(mockContainer.Object);

        mockServiceProvider.Setup(x => x.GetService(typeof(CosmosClient))).Returns(mockCosmosClient.Object);
        mockServiceProvider.Setup(x => x.GetService(typeof(ISendEndpointProvider))).Returns(mockPublishEndpoint.Object);

        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
        mockScopeFactory.Setup(s => s.CreateScope()).Returns(mockScope.Object);
        Mock<IConnectionMultiplexer> redisMock = new();
        redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);

        var cronTask = new DailyPlaylistCronTask(mockLogger.Object, mockScopeFactory.Object, config, redisMock.Object);

        // Act
        var tokenSource = new CancellationTokenSource();
        tokenSource.CancelAfter(TimeSpan.FromSeconds(1));
        await cronTask.StartAsync(tokenSource.Token);
        await Task.Delay(TimeSpan.FromSeconds(1)); // Give it time to start & execute once
        tokenSource.Cancel();

        // Assert
        mockSendEndpoint.Verify(pe => pe.Send(
                It.IsAny<CatalogEnrichmentRequest>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}