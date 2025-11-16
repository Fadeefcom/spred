using System.Net;
using System.Text.Json;
using AggregatorService.Abstractions;
using AggregatorService.Components.Consumers;
using MassTransit;
using Moq;
using Refit;
using Spred.Bus.Contracts;

namespace AggregatorService.Test;

public class VerifyAccountConsumerTests
{
    private readonly Mock<ISpotifyApi> _apiMock = new();
    private readonly Mock<ISpotifyTokenProvider> _tokenProviderMock = new();
    private readonly Mock<IPublishEndpoint> _publishMock = new();

    private VerifyAccountConsumer CreateConsumer()
        => new(_apiMock.Object, _tokenProviderMock.Object, _publishMock.Object);

    private static ConsumeContext<VerifyAccountCommand> CreateContext(VerifyAccountCommand cmd)
    {
        var ctx = new Mock<ConsumeContext<VerifyAccountCommand>>();
        ctx.SetupGet(c => c.Message).Returns(cmd);
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return ctx.Object;
    }

    private static IApiResponse<JsonElement> CreateApiResponse(HttpStatusCode code, string? json = null)
    {
        var content = json != null
            ? JsonDocument.Parse(json).RootElement
            : JsonDocument.Parse("{}").RootElement;

        var mock = new Mock<IApiResponse<JsonElement>>();
        mock.SetupGet(r => r.StatusCode).Returns(code);
        mock.SetupGet(r => r.IsSuccessStatusCode).Returns(code == HttpStatusCode.OK);
        mock.SetupGet(r => r.Content).Returns(content);

        return mock.Object;
    }

    [Fact]
    public async Task Consume_ShouldRotateToken_OnUnauthorized()
    {
        var cmd = new VerifyAccountCommand(Guid.NewGuid(), "acc", AccountPlatform.Spotify, "tok");

        _tokenProviderMock.Setup(t => t.AcquireAsync()).ReturnsAsync(("bearer1", 0));
        _apiMock.Setup(a => a.GetUserPlaylists("bearer1", cmd.AccountId, 50, 0))
            .ReturnsAsync(CreateApiResponse(HttpStatusCode.Unauthorized));
        _tokenProviderMock.Setup(t => t.RotateAsync(0)).ReturnsAsync(("bearer2", 1));
        _apiMock.Setup(a => a.GetUserPlaylists("bearer2", cmd.AccountId, 50, 0))
            .ReturnsAsync(CreateApiResponse(HttpStatusCode.NotFound));

        var consumer = CreateConsumer();
        await consumer.Consume(CreateContext(cmd));

        _publishMock.Verify(p => p.Publish(
            It.Is<VerifyAccountResult>(r => r.Verified == false && r.Error!.Contains("Spotify user profile not found")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldPublishFailure_OnGenericError()
    {
        var cmd = new VerifyAccountCommand(Guid.NewGuid(), "acc", AccountPlatform.Spotify, "tok");

        _tokenProviderMock.Setup(t => t.AcquireAsync()).ReturnsAsync(("bearer", 0));
        _apiMock.Setup(a => a.GetUserPlaylists("bearer", cmd.AccountId, 50, 0))
            .ReturnsAsync(CreateApiResponse(HttpStatusCode.InternalServerError));

        var consumer = CreateConsumer();
        await consumer.Consume(CreateContext(cmd));

        _publishMock.Verify(p => p.Publish(
            It.Is<VerifyAccountResult>(r => r.Verified == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldPublishFailure_WhenNoPlaylists()
    {
        var cmd = new VerifyAccountCommand(Guid.NewGuid(), "acc", AccountPlatform.Spotify, "tok");

        _tokenProviderMock.Setup(t => t.AcquireAsync()).ReturnsAsync(("bearer", 0));
        var json = @"{ ""items"": [] }";
        _apiMock.Setup(a => a.GetUserPlaylists("bearer", cmd.AccountId, 50, 0))
            .ReturnsAsync(CreateApiResponse(HttpStatusCode.OK, json));

        var consumer = CreateConsumer();
        await consumer.Consume(CreateContext(cmd));

        _publishMock.Verify(p => p.Publish(
            It.Is<VerifyAccountResult>(r => r.Verified == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldPublishSuccess_WhenTokenFoundInName()
    {
        var cmd = new VerifyAccountCommand(Guid.NewGuid(), "acc", AccountPlatform.Spotify, "special");

        _tokenProviderMock.Setup(t => t.AcquireAsync()).ReturnsAsync(("bearer", 0));

        var json = @"{
            ""items"": [
                { ""id"": ""pl1"", ""name"": ""my SPECIAL playlist"", ""description"": ""desc"" }
            ]
        }";
        _apiMock.Setup(a => a.GetUserPlaylists("bearer", cmd.AccountId, 50, 0))
            .ReturnsAsync(CreateApiResponse(HttpStatusCode.OK, json));

        var consumer = CreateConsumer();
        await consumer.Consume(CreateContext(cmd));

        _publishMock.Verify(p => p.Publish(
            It.Is<VerifyAccountResult>(r => r.Verified == true &&
                                            r.Proof != null &&
                                            r.AccountId == cmd.AccountId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldPublishSuccess_WhenTokenFoundInDescription()
    {
        var cmd = new VerifyAccountCommand(Guid.NewGuid(), "acc", AccountPlatform.Spotify, "hidden");

        _tokenProviderMock.Setup(t => t.AcquireAsync()).ReturnsAsync(("bearer", 0));

        var json = @"{
            ""items"": [
                { ""id"": ""pl2"", ""name"": ""random"", ""description"": ""this has hidden code"" }
            ]
        }";
        _apiMock.Setup(a => a.GetUserPlaylists("bearer", cmd.AccountId, 50, 0))
            .ReturnsAsync(CreateApiResponse(HttpStatusCode.OK, json));

        var consumer = CreateConsumer();
        await consumer.Consume(CreateContext(cmd));

        _publishMock.Verify(p => p.Publish(
            It.Is<VerifyAccountResult>(r => r.Verified == true && r.Proof != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldPublishNotFoundError_On404()
    {
        var cmd = new VerifyAccountCommand(Guid.NewGuid(), "acc", AccountPlatform.Spotify, "tok");

        _tokenProviderMock.Setup(t => t.AcquireAsync()).ReturnsAsync(("bearer", 0));
        _apiMock.Setup(a => a.GetUserPlaylists("bearer", cmd.AccountId, 50, 0))
            .ReturnsAsync(CreateApiResponse(HttpStatusCode.NotFound));

        var consumer = CreateConsumer();
        await consumer.Consume(CreateContext(cmd));

        _publishMock.Verify(p => p.Publish(
            It.Is<VerifyAccountResult>(r => r.Verified == false && r.Error!.Contains("Spotify user profile not found")),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}