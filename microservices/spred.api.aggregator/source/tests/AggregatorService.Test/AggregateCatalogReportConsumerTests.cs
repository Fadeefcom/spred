using AggregatorService.Abstractions;
using AggregatorService.Components.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Spred.Bus.Contracts;
using StackExchange.Redis;

namespace AggregatorService.Test;

public class AggregateCatalogReportConsumerTests
{
    [Fact]
    public async Task Should_Run_AggregateCatalog_If_Lock_Acquired()
    {
        RedisKey actualKey = default;
        RedisValue actualValue = default;
        TimeSpan? actualExpiry = default;
        
        // Arrange
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();
        redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);
        dbMock.Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<When>()))
            .Callback<RedisKey, RedisValue, TimeSpan?, When>((k, v, t, w) =>
            {
                actualKey = k;
                actualValue = v;
                actualExpiry = t;
            })
            .ReturnsAsync(true);

        var catalogServiceMock = new Mock<ICatalogService>();
        catalogServiceMock.Setup(x => x.CatalogAggregateReport(It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                          .Returns(Task.CompletedTask);

        var loggerFactory = LoggerFactory.Create(_ => { });

        var consumer = new AggregateCatalogReportConsumer(
            catalogServiceMock.Object,
            redisMock.Object,
            loggerFactory
        );

        var message = new AggregateCatalogReport
        {
            Id = Guid.NewGuid(),
            Bucket = 2,
            Data = "2025-07-01",
            Type = "playlistMetadata"
        };

        var contextMock = new Mock<ConsumeContext<AggregateCatalogReport>>();
        contextMock.Setup(x => x.Message).Returns(message);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        Assert.Equal("catalog:inference:lock:playlistMetadata:2:2025-07-01", actualKey);
        Assert.Equal("1", actualValue);
        Assert.Equal(TimeSpan.FromDays(1), actualExpiry);

        // Wait for background task
        await Task.Delay(500);

        catalogServiceMock.Verify(x =>
            x.CatalogAggregateReport(message.Bucket, message.Id, message.Type, message.Data),
            Times.Once);
    }

    [Fact]
    public async Task Should_Skip_If_Lock_Not_Acquired()
    {
        // Arrange
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();
        redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);
        dbMock.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), When.NotExists, It.IsAny<CommandFlags>()))
              .ReturnsAsync(false); // lock not acquired

        var catalogServiceMock = new Mock<ICatalogService>();
        var loggerFactory = LoggerFactory.Create(_ => { });

        var consumer = new AggregateCatalogReportConsumer(
            catalogServiceMock.Object,
            redisMock.Object,
            loggerFactory
        );

        var message = new AggregateCatalogReport
        {
            Id = Guid.NewGuid(),
            Bucket = 2,
            Data = "2025-07-01",
            Type = "playlistMetadata"
        };

        var contextMock = new Mock<ConsumeContext<AggregateCatalogReport>>();
        contextMock.Setup(x => x.Message).Returns(message);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        catalogServiceMock.Verify(x =>
            x.CatalogAggregateReport(It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
    
    [Fact]
    public async Task Should_Run_AggregateCatalog_If_Lock()
    {
        RedisKey actualKey = default;
        RedisValue actualValue = default;
        TimeSpan? actualExpiry = default;
        
        // Arrange
        var redisMock = new Mock<IConnectionMultiplexer>();
        var dbMock = new Mock<IDatabase>();
        redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);
        dbMock.Setup(x => x.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<When>()))
            .Callback<RedisKey, RedisValue, TimeSpan?, When>((k, v, t, w) =>
            {
                actualKey = k;
                actualValue = v;
                actualExpiry = t;
            })
            .ReturnsAsync(false);

        var catalogServiceMock = new Mock<ICatalogService>();
        catalogServiceMock.Setup(x => x.CatalogAggregateReport(It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                          .Returns(Task.CompletedTask);

        var loggerFactory = LoggerFactory.Create(_ => { });

        var consumer = new AggregateCatalogReportConsumer(
            catalogServiceMock.Object,
            redisMock.Object,
            loggerFactory
        );

        var message = new AggregateCatalogReport
        {
            Id = Guid.NewGuid(),
            Bucket = 2,
            Data = "2025-07-01",
            Type = "playlistMetadata"
        };

        var contextMock = new Mock<ConsumeContext<AggregateCatalogReport>>();
        contextMock.Setup(x => x.Message).Returns(message);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        Assert.Equal("catalog:inference:lock:playlistMetadata:2:2025-07-01", actualKey);
        Assert.Equal("1", actualValue);
        Assert.Equal(TimeSpan.FromDays(1), actualExpiry);
    }
}
