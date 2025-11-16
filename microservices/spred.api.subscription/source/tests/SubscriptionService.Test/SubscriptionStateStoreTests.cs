using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Repository.Abstractions.Components;
using Repository.Abstractions.Interfaces;
using StackExchange.Redis;
using SubscriptionService.Components;
using SubscriptionService.Models.Entities;
using SubscriptionService.Test.Fixtures;

namespace SubscriptionService.Test;

public class SubscriptionStateStoreTests
{
    private readonly Mock<IPersistenceStore<UserSubscriptionStatus, Guid>> _storeMock;
    private readonly Mock<IPersistenceStore<SubscriptionSnapshot, Guid>> _snapshotStoreMock;
    private readonly Mock<IConnectionMultiplexer> _connectionMock;
    private readonly Mock<TransactionalBatch> _batchMock;
    private readonly Mock<TransactionalBatchResponse> _batchResponseMock;
    private readonly Mock<IDatabase> _redisMock;
    private readonly Mock<Container> _containerMock;
    private readonly SubscriptionStateStore _service;
    private readonly Guid _userId = Guid.NewGuid();

    public SubscriptionStateStoreTests()
    {
        _storeMock = new Mock<IPersistenceStore<UserSubscriptionStatus, Guid>>();
        _snapshotStoreMock = new Mock<IPersistenceStore<SubscriptionSnapshot, Guid>>();
        _connectionMock = new Mock<IConnectionMultiplexer>();
        _redisMock = new Mock<IDatabase>();
        _connectionMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_redisMock.Object);

        _containerMock = new Mock<Container>();
        _batchMock = new Mock<TransactionalBatch>();
        _batchResponseMock = new Mock<TransactionalBatchResponse>();
        var cosmosContainer = new CosmosContainer<UserSubscriptionStatus>()
        {
            Container = _containerMock.Object
        };

        // применяем SetupPersistenceStoreMock
        SubscriptionApiFactory.SetupPersistenceStoreMock<UserSubscriptionStatus, Guid, long>(
            _storeMock,
            () => new UserSubscriptionStatus
            {
                UserId = _userId,
                IsActive = true,
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(3)
            });

        SubscriptionApiFactory.SetupPersistenceStoreMock<SubscriptionSnapshot, Guid, long>(
            _snapshotStoreMock,
            () => new SubscriptionSnapshot
            {
                UserId = _userId,
                Kind = "test",
                ExternalId = "ext",
                RawJson = "{}"
            });

        _service = new SubscriptionStateStore(
            _storeMock.Object,
            _snapshotStoreMock.Object,
            cosmosContainer,
            _connectionMock.Object,
            NullLoggerFactory.Instance);
    }

    [Fact]
    public async Task GetStatusAsync_ShouldReturnActive()
    {
        var result = await _service.GetStatusAsync(_userId, CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task SetStatusAsync_ShouldWriteRedis_WhenValid()
    {
        var result = await _service.SetStatusAsync(
            _userId,
            "payment_1",
            true,
            "sub_1",
            null,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1),
            CancellationToken.None);

        Assert.NotNull(result);
        _redisMock.Verify(x =>
            x.StringSetAsync($"subscription:{_userId}", true, It.IsAny<TimeSpan?>(), false, When.Always, CommandFlags.None),
            Times.Once);
    }

    [Fact]
    public async Task GetDetailsAsync_ShouldReturnEntity()
    {
        var details = await _service.GetDetailsAsync(_userId);
        Assert.NotNull(details);
        Assert.True(details.IsActive);
    }

    [Fact]
    public async Task SaveSnapshotAsync_ShouldReturnId()
    {
        var id = await _service.SaveSnapshotAsync(_userId, Guid.NewGuid(), "kind", "ext", "{}", CancellationToken.None);
        Assert.NotNull(id);
    }

     [Fact]
    public async Task SaveAtomicAsync_ShouldReturnFailure_WhenBatchFails()
    {
        // имитация TransactionalBatch.ExecuteAsync
        _containerMock
            .Setup(c => c.CreateTransactionalBatch(It.IsAny<PartitionKey>()))
            .Returns(_batchMock.Object);

        _batchMock.Setup(b => b.UpsertItem(It.IsAny<UserSubscriptionStatus>(), It.IsAny<TransactionalBatchItemRequestOptions>())).Returns(_batchMock.Object);
        _batchMock.Setup(b => b.UpsertItem(It.IsAny<SubscriptionSnapshot>(), It.IsAny<TransactionalBatchItemRequestOptions>())).Returns(_batchMock.Object);
        _batchMock.Setup(b => b.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_batchResponseMock.Object);

        _batchResponseMock.SetupGet(r => r.IsSuccessStatusCode).Returns(false);
        _batchResponseMock.SetupGet(r => r.StatusCode).Returns(HttpStatusCode.BadRequest);
        _batchResponseMock.SetupGet(r => r.ErrorMessage).Returns("Simulated failure");

        var result = await _service.SaveAtomicAsync(
            _userId,
            new UserSubscriptionStatus { UserId = _userId },
            "k",
            "ext",
            "{}",
            CancellationToken.None);

        Assert.False(result.StatusSaved);
        Assert.False(result.SnapshotSaved);
        Assert.Equal(HttpStatusCode.BadRequest, result.HttpStatus);
    }

    [Fact]
    public async Task SaveAtomicAsync_ShouldReturnSuccess_WhenBatchOk()
    {
        var userStatus = new UserSubscriptionStatus { UserId = _userId };

        _containerMock
            .Setup(c => c.CreateTransactionalBatch(It.IsAny<PartitionKey>()))
            .Returns(_batchMock.Object);

        _batchMock.Setup(b => b.UpsertItem(It.IsAny<UserSubscriptionStatus>(), It.IsAny<TransactionalBatchItemRequestOptions>())).Returns(_batchMock.Object);
        _batchMock.Setup(b => b.UpsertItem(It.IsAny<SubscriptionSnapshot>(), It.IsAny<TransactionalBatchItemRequestOptions>())).Returns(_batchMock.Object);
        _batchMock.Setup(b => b.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_batchResponseMock.Object);

        _batchResponseMock.SetupGet(r => r.IsSuccessStatusCode).Returns(true);
        _batchResponseMock.SetupGet(r => r.StatusCode).Returns(HttpStatusCode.OK);
        
        var userResultMock = Mock.Of<TransactionalBatchOperationResult<UserSubscriptionStatus>>(
            r => r.StatusCode == System.Net.HttpStatusCode.OK);
        var snapResultMock = Mock.Of<TransactionalBatchOperationResult<SubscriptionSnapshot>>(
            r => r.StatusCode == System.Net.HttpStatusCode.OK);
        
        _batchResponseMock
            .Setup(r => r.GetOperationResultAtIndex<UserSubscriptionStatus>(0))
            .Returns(userResultMock);

        _batchResponseMock
            .Setup(r => r.GetOperationResultAtIndex<SubscriptionSnapshot>(1))
            .Returns(snapResultMock);

        var result = await _service.SaveAtomicAsync(
            _userId,
            userStatus,
            "k",
            "ext",
            "{}",
            CancellationToken.None);

        Assert.True(result.StatusSaved);
        Assert.True(result.SnapshotSaved);
        Assert.Equal(HttpStatusCode.OK, result.HttpStatus);
    }
}
