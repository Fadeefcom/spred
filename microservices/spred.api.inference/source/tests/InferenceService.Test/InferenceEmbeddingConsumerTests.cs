using System.Net;
using System.Text.Json;
using InferenceService.Abstractions;
using InferenceService.Components.Consumers;
using InferenceService.Configuration;
using InferenceService.Models.Dto;
using InferenceService.Models.Entities;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Refit;
using Spred.Bus.Contracts;
using StackExchange.Redis;

namespace InferenceService.Test;

public class InferenceEmbeddingConsumerTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();
    private readonly Mock<IDatabase> _dbMock = new();
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<IVectorSearch> _vectorMock = new();
    private readonly Mock<IInferenceManager> _inferenceManagerMock = new();
    private readonly Mock<ISendEndpointProvider> _sendProviderMock = new();
    private readonly Mock<ISendEndpoint> _sendEndpointMock = new();

    private readonly InferenceEmbeddingConsumer _consumer;

    public InferenceEmbeddingConsumerTests()
    {
        var loggerMock = new Mock<ILogger<InferenceEmbeddingConsumer>>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);
        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), null)).Returns(_dbMock.Object);
        _sendProviderMock.Setup(x => x.GetSendEndpoint(It.IsAny<Uri>()))
            .ReturnsAsync(_sendEndpointMock.Object);

        var options = Options.Create(new ModelVersion { Version = "v1.0.0", Threshold = 0.1f });

        _consumer = new InferenceEmbeddingConsumer(
            _loggerFactoryMock.Object,
            _redisMock.Object,
            _vectorMock.Object,
            _inferenceManagerMock.Object,
            _sendProviderMock.Object,
            options
        );
    }

    [Fact]
    public async Task Consume_ShouldProcessEmbedding_Success()
    {
        var trackId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var json = JsonDocument.Parse("""
        {
          "results": [
            {
              "catalogId": "11111111-1111-1111-1111-111111111111",
              "catalogOwner": "22222222-2222-2222-2222-222222222222",
              "catalogType": "playlist",
              "score": 80,
              "topnSimilarTracks": []
            }
          ],
          "genres": ["rock"],
          "topnSimilarTracksOverall": []
        }
        """).RootElement;

        var apiResponse = new ApiResponse<JsonElement>(
            new HttpResponseMessage(HttpStatusCode.OK),
            json,
            new RefitSettings()
        );

        _vectorMock.Setup(x => x.SearchCatalogs(It.IsAny<SearchQuery>())).ReturnsAsync(apiResponse);

        var message = new TrackEmbeddingResult
        {
            TrackId = trackId,
            SpredUserId = userId,
            Success = true,
            Embedding = new float[10]
        };
        var context = Mock.Of<ConsumeContext<TrackEmbeddingResult>>(x => x.Message == message);

        await _consumer.Consume(context);

        _inferenceManagerMock.Verify(x =>
            x.SaveInference(It.IsAny<List<InferenceMetadata>>(),
                trackId,
                userId,
                "v1.0.0",
                It.IsAny<CancellationToken>()), Times.Once);

        _sendEndpointMock.Verify(x =>
            x.Send(It.IsAny<TrackUpdateRequest>(), It.IsAny<CancellationToken>()), Times.Once);

        _dbMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            "completed",
            It.IsAny<TimeSpan>(),
            When.Exists,
            CommandFlags.FireAndForget), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldExit_WhenSuccessFalse()
    {
        var message = new TrackEmbeddingResult
        {
            TrackId = Guid.NewGuid(),
            SpredUserId = Guid.NewGuid(),
            Success = false,
            ErrorMessage = "bad input"
        };
        var context = Mock.Of<ConsumeContext<TrackEmbeddingResult>>(x => x.Message == message);

        await _consumer.Consume(context);

        _inferenceManagerMock.Verify(x => x.SaveInference(It.IsAny<List<InferenceMetadata>>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _sendEndpointMock.Verify(x =>
            x.Send(It.IsAny<TrackUpdateRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        _dbMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            "failed",
            It.IsAny<TimeSpan>(),
            When.Exists,
            CommandFlags.FireAndForget), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldExit_WhenVectorSearchFails()
    {
        var failedResponse = new ApiResponse<JsonElement>(
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            JsonDocument.Parse("{}").RootElement,
            new RefitSettings()
        );
        _vectorMock.Setup(x => x.SearchCatalogs(It.IsAny<SearchQuery>())).ReturnsAsync(failedResponse);

        var message = new TrackEmbeddingResult
        {
            TrackId = Guid.NewGuid(),
            SpredUserId = Guid.NewGuid(),
            Success = true,
            Embedding = new float[5]
        };
        var context = Mock.Of<ConsumeContext<TrackEmbeddingResult>>(x => x.Message == message);

        await _consumer.Consume(context);

        _inferenceManagerMock.Verify(x => x.SaveInference(It.IsAny<List<InferenceMetadata>>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _dbMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            "failed",
            It.IsAny<TimeSpan>(),
            When.Exists,
            CommandFlags.FireAndForget), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldSetRedisFailed_OnException()
    {
        _vectorMock.Setup(x => x.SearchCatalogs(It.IsAny<SearchQuery>()))
            .Throws(new System.Exception("boom"));

        var message = new TrackEmbeddingResult
        {
            TrackId = Guid.NewGuid(),
            SpredUserId = Guid.NewGuid(),
            Success = true,
            Embedding = new float[5]
        };
        var context = Mock.Of<ConsumeContext<TrackEmbeddingResult>>(x => x.Message == message);

        await Assert.ThrowsAsync<System.Exception>(() => _consumer.Consume(context));

        _dbMock.Verify(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            "failed",
            It.IsAny<TimeSpan>(),
            When.Exists,
            CommandFlags.FireAndForget), Times.Once);
    }
}
