using System.Net;
using AggregatorService.Components;
using AggregatorService.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;

namespace AggregatorService.Test;

public class CatalogServiceTests
{
    private readonly Mock<Container> _catalogContainer = new();
    private readonly Mock<Container> _trackContainer = new();
    private readonly Mock<Container> _enrichmentContainer = new();
    private readonly Mock<ILogger<CatalogService>> _logger = new();
    private readonly Mock<IConfiguration> _configuration = new();

    private CatalogService CreateService()
    {
        var cosmosClient = new Mock<CosmosClient>();
        cosmosClient
            .Setup(x => x.GetContainer(It.IsAny<string>(), "CatalogMetadata_v2"))
            .Returns(_catalogContainer.Object);
        cosmosClient
            .Setup(x => x.GetContainer(It.IsAny<string>(), "TrackMetadata_v2"))
            .Returns(_trackContainer.Object);
        cosmosClient
            .Setup(x => x.GetContainer(It.IsAny<string>(), "CatalogEnrichment_v1"))
            .Returns(_enrichmentContainer.Object);

        _configuration.Setup(x => x["DbConnectionOptions:DatabaseName"]).Returns("test-db");
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(x => x.EnvironmentName).Returns("Test");

        return new CatalogService(env.Object, cosmosClient.Object, _configuration.Object, _logger.Object);
    }

    [Fact]
    public async Task RunAggregateCatalog_Should_Process_And_Save_Data()
    {
        // Arrange
        var service = CreateService();
        var enrichmentId = Guid.NewGuid();

        // Setup catalog metadata
        var catalogItem = JObject.Parse("""
        {
            "id": "00000000-0000-0000-0000-000000000001",
            "SpredUserId": "00000000-0000-0000-0000-000000000002",
            "Tracks": ["00000000-0000-0000-0000-000000000010", "00000000-0000-0000-0000-000000000011"],
            "Type": "playlistMetadata"
        }
        """);

        var catalogIterator = new Mock<FeedIterator<JObject>>();
        catalogIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
        catalogIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(FeedResponse(catalogItem));
        _catalogContainer.Setup(x => x.GetItemQueryIterator<JObject>(
            It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>())
        ).Returns(catalogIterator.Object);

        // Setup valid tracks
        var track1 = JObject.Parse("""
        {
            "id": "00000000-0000-0000-0000-000000000010",
            "Status": "1",
            "Genre": "rock"
        }
        """);

        var track2 = JObject.Parse("""
        {
            "id": "00000000-0000-0000-0000-000000000011",
            "Status": "1",
            "Genre": "jazz"
        }
        """);

        var myItems = new List<JObject>
        {
            track1, track2
        };
        var feedResponseMock = new Mock<FeedResponse<JObject>>();
        feedResponseMock.Setup(x => x.GetEnumerator()).Returns(myItems.GetEnumerator());

        _trackContainer.Setup(x => x.ReadManyItemsAsync<JObject>(
            It.IsAny<IReadOnlyList<(string id, PartitionKey pk)>>(),
            It.IsAny<ReadManyRequestOptions>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(feedResponseMock.Object);

        _enrichmentContainer.Setup(x => x.CreateItemAsync(
            It.IsAny<CatalogInference>(),
            It.IsAny<PartitionKey>(),
            null,
            It.IsAny<CancellationToken>())
        ).ReturnsAsync(() => null!);

        // Act
        await service.CatalogAggregateReport(bucket: 5, id: enrichmentId, type: "playlistMetadata", shortDate: "2025-07-31");

        // Assert
        _enrichmentContainer.Verify(x => x.CreateItemAsync(
            It.Is<CatalogInference>(p => p.catalogInferenceResponces.Count == 1),
            It.IsAny<PartitionKey>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAggregateCatalog_Should_Throw_If_Cosmos_Error()
    {
        // Arrange
        var service = CreateService();
        var enrichmentId = Guid.NewGuid();

        var catalogItem = JObject.Parse("""
        {
            "id": "00000000-0000-0000-0000-000000000001",
            "SpredUserId": "00000000-0000-0000-0000-000000000002",
            "Tracks": ["00000000-0000-0000-0000-000000000010"]
        }
        """);

        var catalogIterator = new Mock<FeedIterator<JObject>>();
        catalogIterator.SetupSequence(x => x.HasMoreResults).Returns(true).Returns(false);
        catalogIterator.Setup(x => x.ReadNextAsync(default)).ReturnsAsync(FeedResponse(catalogItem));
        _catalogContainer.Setup(x => x.GetItemQueryIterator<JObject>(
            It.IsAny<QueryDefinition>(), null, It.IsAny<QueryRequestOptions>())
        ).Returns(catalogIterator.Object);

        var track = JObject.Parse("""
        {
            "id": "00000000-0000-0000-0000-000000000010",
            "Status": "1"
        }
        """);
        
        var myItems = new List<JObject>
        {
            track
        };
        var feedResponseMock = new Mock<FeedResponse<JObject>>();
        feedResponseMock.Setup(x => x.GetEnumerator()).Returns(myItems.GetEnumerator());

        _trackContainer.Setup(x => x.ReadManyItemsAsync<JObject>(
            It.IsAny<IReadOnlyList<(string id, PartitionKey pk)>>(),
            It.IsAny <ReadManyRequestOptions>(), It.IsAny<CancellationToken>())
        ).ReturnsAsync(feedResponseMock.Object);

        _enrichmentContainer.Setup(x => x.CreateItemAsync(
            It.IsAny<CatalogInference>(),
            It.IsAny<PartitionKey>(),
            null,
            It.IsAny<CancellationToken>())
        ).ThrowsAsync(new CosmosException("Write failed", HttpStatusCode.ServiceUnavailable, 0, "", 0));

        // Act + Assert
        await Assert.ThrowsAsync<CosmosException>(() =>
            service.CatalogAggregateReport(bucket: 5, id: enrichmentId, type: "playlistMetadata", shortDate: "2025-07-31"));
    }

    private static FeedResponse<T> FeedResponse<T>(params T[] items)
    {
        var mock = new Mock<FeedResponse<T>>();
        mock.Setup(x => x.GetEnumerator()).Returns(items.ToList().GetEnumerator());
        mock.Setup(x => x.Resource).Returns(items.ToList());
        return mock.Object;
    }
}
