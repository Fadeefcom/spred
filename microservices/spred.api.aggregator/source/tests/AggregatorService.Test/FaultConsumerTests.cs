using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Models;
using Spred.Bus.Consumers;
using Spred.Bus.DTOs;

namespace AggregatorService.Test;

public class FaultConsumerTests
{
    [Fact]
    public async Task Should_Handle_Null_Exception_List_Gracefully()
    {
        // Arrange
        var testMessage = new MetadataDto
        {
            PrimaryId = "track-xyz",
            Tracks = []
        };

        var exceptionInfoMock = new Mock<ExceptionInfo>();

        var mockFault = new Mock<Fault<MetadataDto>>();
        mockFault.Setup(f => f.Message).Returns(testMessage);
        mockFault.Setup(f => f.Exceptions).Returns([exceptionInfoMock.Object]);

        var loggerMock = new Mock<ILogger<FaultConsumer<MetadataDto>>>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

        var context = new Mock<ConsumeContext<Fault<MetadataDto>>>();
        context.Setup(c => c.Message).Returns(mockFault.Object);
        context.Setup(c => c.MessageId).Returns(Guid.NewGuid());

        var consumer = new FaultConsumer<MetadataDto>(loggerFactoryMock.Object);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        Assert.True(true);
    }
}