using MassTransit;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Models;
using Spred.Bus.Contracts;
using TrackService.Components.Consumers;
using TrackService.Models.Entities;
using TrackService.Test.Helpers;

namespace TrackService.Test;

public class TrackUpdateConsumerTests
{
    private readonly Mock<IPersistenceStore<TrackMetadata, Guid>> _storeMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();
    private readonly Mock<ILogger<TrackUpdateConsumer>> _loggerMock = new();

    private readonly TrackUpdateConsumer _consumer;

    public TrackUpdateConsumerTests()
    {
        _loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _consumer = new TrackUpdateConsumer(_storeMock.Object, _loggerFactoryMock.Object);
    }

    [Fact]
    public async Task Consume_ShouldUpdateTrack_WhenGenreIsNotEmpty()
    {
        // Arrange
        var trackId = Guid.NewGuid();
        var spredUserId = Guid.NewGuid();
        var newGenre = "Electronic";

        var metadata = new TrackMetadata();
        ReflectionHelper.SetProtectedProperty(metadata, nameof(TrackMetadata.SpredUserId), Guid.NewGuid());
        ReflectionHelper.SetProtectedProperty(metadata, nameof(TrackMetadata.Bucket), "01");
        ReflectionHelper.SetProtectedProperty(metadata, nameof(TrackMetadata.Title), "Test Track");
        ReflectionHelper.SetProtectedProperty(metadata, nameof(TrackMetadata.Popularity), (uint)50);

        var contextMock = new Mock<ConsumeContext<TrackUpdateRequest>>();
        contextMock.Setup(x => x.Message).Returns(new TrackUpdateRequest
        {
            TrackId = trackId,
            SpredUserId = spredUserId,
            Genre = newGenre
        });

        _storeMock
            .Setup(s => s.GetAsync(trackId, It.IsAny<PartitionKey>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new PersistenceResult<TrackMetadata>(metadata, false, null));

        _storeMock
            .Setup(s => s.UpdateAsync(It.IsAny<TrackMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null))
            .Callback<TrackMetadata, CancellationToken>((m, _) =>
            {
                // Симулируем поведение обновления: Genre должно быть заменено
                Assert.Equal(newGenre, m.Audio.Genre);
            });

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        _storeMock.Verify(s => s.GetAsync(trackId, It.IsAny<PartitionKey>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Once);
        _storeMock.Verify(s => s.UpdateAsync(It.IsAny<TrackMetadata>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldNotCallUpdate_WhenGenreIsNull()
    {
        // Arrange
        var contextMock = new Mock<ConsumeContext<TrackUpdateRequest>>();
        contextMock.Setup(x => x.Message).Returns(new TrackUpdateRequest
        {
            TrackId = Guid.NewGuid(),
            SpredUserId = Guid.NewGuid(),
            Genre = null
        });

        // Act
        await _consumer.Consume(contextMock.Object);

        // Assert
        _storeMock.Verify(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<PartitionKey>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Never);
        _storeMock.Verify(x => x.UpdateAsync(It.IsAny<TrackMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
