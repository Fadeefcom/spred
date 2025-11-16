using System.Net;
using System.Net.Http.Json;
using InferenceService.Abstractions;
using InferenceService.Models.Dto;
using InferenceService.Models.Entities;
using InferenceService.Test.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StackExchange.Redis;

namespace InferenceService.Test;

public class InferenceRoutesTests : IClassFixture<InferenceApiFactory>
{
    private readonly InferenceApiFactory _factory;
    private readonly HttpClient _client;

    public InferenceRoutesTests(InferenceApiFactory factory)
    {
        _factory = factory;
        factory.EnableTestAuth = true;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetInferenceById_ReturnsOk()
    {
        var result = await _client.GetAsync($"/inference/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var content = await result.Content.ReadFromJsonAsync<InferenceResultDto>();
        Assert.NotNull(content);
        Assert.NotNull(content.Metadata);
    }

    [Fact]
    public async Task GetInferenceById_WithLimitExceed_ReturnsBadRequest()
    {
        var result = await _client.GetAsync($"/inference/{Guid.NewGuid()}?limit=20");
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task GetInferenceStatus_ReturnsOk_WhenStatusExists()
    {
        var spredUserId = Guid.Empty;
        var trackId = Guid.NewGuid();
        var key = $"inference:{trackId}:{spredUserId}";

        var dbMock = _factory.RedisDbMock;
        dbMock.Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
              .ReturnsAsync("processing");

        var result = await _client.GetAsync($"/inference/status/{trackId}");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetInferenceStatus_ReturnsNotFound_WhenNotInRedis()
    {
        var result = await _client.GetAsync($"/inference/status/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task PatchRate_ReturnsNoContent()
    {
        var request = new UpdateRateRequest
        {
            ModelVersion = "v1.0.0",
            IsLiked = true,
            HasApplied = true,
            WasAccepted = false
        };

        var playlistId = Guid.NewGuid();
        var trackId = Guid.NewGuid();

        var managerMock = new Mock<IInferenceManager>();
        managerMock.Setup(x => x.AddRateToPlaylist(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<ReactionStatus>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IInferenceManager));
                services.AddScoped(_ => managerMock.Object);
            });
        }).CreateClient();

        var response = await client.PatchAsJsonAsync($"/inference/rate/{trackId}/{playlistId}", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PatchRate_ReturnsBadRequest_IfInvalidModelVersion()
    {
        var request = new UpdateRateRequest
        {
            ModelVersion = "v1.invalid",
            IsLiked = true,
            HasApplied = false,
            WasAccepted = false
        };

        var playlistId = Guid.NewGuid();
        var trackId = Guid.NewGuid();

        var response = await _client.PatchAsJsonAsync($"/inference/rate/{trackId}/{playlistId}", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}