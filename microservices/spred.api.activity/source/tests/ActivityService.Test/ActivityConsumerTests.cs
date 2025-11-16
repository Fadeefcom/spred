using System.Text.Json;
using ActivityService.Components.Consumers;
using ActivityService.Models;
using MassTransit;
using Microsoft.Azure.Cosmos;
using Moq;
using Repository.Abstractions.Components;
using Spred.Bus.Contracts;

public class ActivityConsumerTests
{
    private readonly Mock<Container> _containerMock;
    private readonly ActivityConsumer _consumer;

    public ActivityConsumerTests()
    {
        _containerMock = new Mock<Container>();
        _consumer = new ActivityConsumer(new CosmosContainer<ActivityEntity>
        {
            Container = _containerMock.Object
        });
    }

    [Fact]
    public async Task Consume_ShouldCreateItem_WhenNoExistingSequence()
    {
        var iteratorMock = SetupIterator(new List<int?> { null });

        _containerMock
            .Setup(c => c.GetItemQueryIterator<int?>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string?>(),
                It.IsAny<QueryRequestOptions?>()))
            .Returns(iteratorMock.Object);

        _containerMock
            .Setup(c => c.CreateItemAsync(
                It.IsAny<ActivityEntity>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActivityEntity entity, PartitionKey? pk, ItemRequestOptions? ro, CancellationToken ct) =>
            {
                Assert.Equal(1, entity.Sequence);
                Assert.Equal("track", entity.ObjectType);
                Assert.Equal("created", entity.Verb);
                return Mock.Of<ItemResponse<ActivityEntity>>();
            });

        var record = CreateRecord();
        var context = Mock.Of<ConsumeContext<ActivityRecord>>(x => x.Message == record);

        await _consumer.Consume(context);

        _containerMock.Verify(c => c.CreateItemAsync(
            It.IsAny<ActivityEntity>(),
            It.IsAny<PartitionKey?>(),
            It.IsAny<ItemRequestOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldIncrementSequence_WhenExistingSequenceExists()
    {
        var iteratorMock = SetupIterator(new List<int?> { 5 });

        _containerMock
            .Setup(c => c.GetItemQueryIterator<int?>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string?>(),
                It.IsAny<QueryRequestOptions?>()))
            .Returns(iteratorMock.Object);

        _containerMock
            .Setup(c => c.CreateItemAsync(
                It.IsAny<ActivityEntity>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActivityEntity entity, PartitionKey? pk, ItemRequestOptions? ro, CancellationToken ct) =>
            {
                Assert.Equal(6, entity.Sequence);
                Assert.Equal("activity.track.created", entity.MessageKey);
                Assert.NotNull(entity.Args);
                Assert.Equal(ActivityImportance.Normal, entity.Importance);
                return Mock.Of<ItemResponse<ActivityEntity>>();
            });

        var record = CreateRecord();
        var context = Mock.Of<ConsumeContext<ActivityRecord>>(x => x.Message == record);

        await _consumer.Consume(context);
    }

    [Fact]
    public async Task Consume_ShouldHandleMultiplePagesIterator()
    {
        var iteratorMock = new Mock<FeedIterator<int?>>();
        iteratorMock.SetupSequence(x => x.HasMoreResults)
            .Returns(true)
            .Returns(true)
            .Returns(false);
        iteratorMock.SetupSequence(x => x.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(MockFeedResponse(new List<int?> { 2 }))
            .ReturnsAsync(MockFeedResponse(new List<int?> { 8 }));

        _containerMock
            .Setup(c => c.GetItemQueryIterator<int?>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string?>(),
                It.IsAny<QueryRequestOptions?>()))
            .Returns(iteratorMock.Object);

        _containerMock
            .Setup(c => c.CreateItemAsync(
                It.IsAny<ActivityEntity>(),
                It.IsAny<PartitionKey?>(),
                It.IsAny<ItemRequestOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActivityEntity entity, PartitionKey? pk, ItemRequestOptions? ro, CancellationToken ct) =>
            {
                Assert.Equal(9, entity.Sequence);
                return Mock.Of<ItemResponse<ActivityEntity>>();
            });

        var record = CreateRecord();
        var context = Mock.Of<ConsumeContext<ActivityRecord>>(x => x.Message == record);

        await _consumer.Consume(context);
    }

    private static FeedResponse<T> MockFeedResponse<T>(IEnumerable<T> items)
    {
        var response = new Mock<FeedResponse<T>>();
        response.Setup(r => r.GetEnumerator()).Returns(items.GetEnumerator());
        return response.Object;
    }

    private static Mock<FeedIterator<int?>> SetupIterator(IEnumerable<int?> values)
    {
        var iteratorMock = new Mock<FeedIterator<int?>>();
        iteratorMock.SetupSequence(i => i.HasMoreResults)
            .Returns(true)
            .Returns(false);
        iteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(MockFeedResponse(values));
        return iteratorMock;
    }

    private static ActivityRecord CreateRecord()
    {
        return new ActivityRecord(
            Id: Guid.NewGuid(),
            ActorUserId: Guid.NewGuid(),
            ObjectType: "track",
            ObjectId: Guid.NewGuid(),
            Verb: "created",
            MessageKey: "activity.track.created",
            MessageArgs: new Dictionary<string, object?> { { "name", "test" } },
            Before: null,
            After: JsonDocument.Parse("{\"name\":\"after\"}").RootElement,
            CorrelationId: Guid.NewGuid().ToString(),
            Service: "ActivityService",
            Audience: "public",
            Importance: ActivityImportance.Normal,
            OwnerUserId: Guid.NewGuid(),
            OtherPartyUserId: null,
            Tags: new[] { "tag1", "tag2" },
            CreatedAt: DateTimeOffset.UtcNow
        );
    }
}
