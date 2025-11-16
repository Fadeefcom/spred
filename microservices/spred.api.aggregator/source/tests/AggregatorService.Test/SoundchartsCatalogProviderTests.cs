using System.Text.Json;
using AggregatorService.Abstractions;
using AggregatorService.Components;
using AggregatorService.Configurations;
using AggregatorService.Models;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Refit;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;

namespace AggregatorService.Test;

public class SoundchartsCatalogProviderTests
{
    private readonly Mock<ISoundchartsApi> _apiMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IApiRateLimiter> _rateLimiterMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();
    private readonly Mock<ILogger<SoundchartsCatalogProvider>> _loggerMock = new();
    private readonly SoundchartsCatalogProvider _provider;
    private readonly Mock<IMemoryCache> _cacheMock = new();

    public SoundchartsCatalogProviderTests()
    {
        var options = Options.Create(new SoundchartsOptions
        {
            AppId = "app123",
            ApiKey = "key456"
        });

        _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _provider = new SoundchartsCatalogProvider(
            _apiMock.Object,
            _mapperMock.Object,
            _cacheMock.Object,
            options,
            _loggerFactoryMock.Object,
            _rateLimiterMock.Object);
    }

    private static IApiResponse<JsonElement> MakeResponse(JsonElement element, bool success = true)
    {
        var mock = new Mock<IApiResponse<JsonElement>>();
        mock.Setup(r => r.IsSuccessStatusCode).Returns(success);
        mock.Setup(r => r.IsSuccessful).Returns(success);
        mock.Setup(r => r.Content).Returns(element);
        return mock.Object;
    }

    [Fact]
    public async Task ResolvePlaylistIdAsync_ShouldReturnUuid_WhenResponseValid()
    {
        var json = JsonDocument.Parse(@"{ ""object"": { ""uuid"": ""pl123"" } }");
        var response = MakeResponse(json.RootElement);

        _apiMock.Setup(a => a.GetPlaylistByPlatformIdAsync("app123", "key456", "spotify", "abc", null))
            .ReturnsAsync(response);
        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response);

        var result = await _provider.ResolvePlaylistIdAsync("spotify:playlist:abc", "spotify");

        Assert.Equal("pl123", result);
    }

    [Fact]
    public async Task ResolvePlaylistIdAsync_ShouldReturnNull_WhenInvalidInput()
    {
        var result = await _provider.ResolvePlaylistIdAsync("", "spotify");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPlaylistMetadataAsync_ShouldReturnMappedMetadata()
    {
        var json = JsonDocument.Parse(@"{ ""uuid"": ""p1"", ""title"": ""Playlist"" }");
        var response = MakeResponse(json.RootElement);
        var mapped = new MetadataDto { PrimaryId = "p1", Name = "Playlist" };

        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response);
        _mapperMock.Setup(m => m.Map<MetadataDto>(It.IsAny<SoundchartsPlaylistWrapper>())).Returns(mapped);

        var result = await _provider.GetPlaylistMetadataAsync("p1", "spotify");

        Assert.NotNull(result);
        Assert.Equal("p1", result!.PrimaryId);
    }

    [Fact]
    public async Task GetPlaylistStatsAsync_ShouldReturnMappedStats()
    {
        var json = JsonDocument.Parse(@"{ ""items"": [ { ""date"": ""2025-01-01T00:00:00Z"", ""value"": 77 } ] }");
        var response = MakeResponse(json.RootElement);

        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response);

        _apiMock.Setup(a => a.GetPlaylistAudienceAsync("id", "app123", "key456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _provider.GetPlaylistStatsAsync("id", "spotify", true);

        Assert.Single(result);
        Assert.Equal(77u, result.First().Value);
    }

    [Fact]
    public async Task GetPlaylistStatsAsync_ShouldReturnEmpty_WhenUpdateStatsFalse()
    {
        var result = await _provider.GetPlaylistStatsAsync("id", "spotify", false);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPlaylistStatsAsync_ShouldReturnEmpty_WhenResponseInvalid()
    {
        var response = MakeResponse(default, success: false);
        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response);

        var result = await _provider.GetPlaylistStatsAsync("id", "spotify", true);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPlaylistTracksSnapshotAsync_ShouldReturnMappedTracks()
    {
        var json = JsonDocument.Parse(@"
        {
            ""items"": [
                { ""song"": { ""uuid"": ""song1"" }, ""entryDate"": ""2025-01-01T00:00:00Z"" }
            ]
        }");
        var response = MakeResponse(json.RootElement);

        var songResponse = MakeResponse(JsonDocument.Parse(@"{ ""uuid"": ""song1"", ""title"": ""TestSong"" }").RootElement);
        var identifiersResponse = MakeResponse(JsonDocument.Parse(@"{ ""items"": [ { ""platformCode"": ""spotify"", ""identifier"": ""id123"", ""url"": ""https://test"" } ] }").RootElement);

        _rateLimiterMock.SetupSequence(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response)
            .ReturnsAsync(songResponse)
            .ReturnsAsync(identifiersResponse);

        _mapperMock.Setup(m => m.Map<TrackDtoWithPlatformIds>(It.IsAny<SoundchartsTrackWrapper>()))
            .Returns(new TrackDtoWithPlatformIds { Title = "TestSong", PrimaryId = "id123" });

        var result = await _provider.GetPlaylistTracksSnapshotAsync("pid", "spotify", DateTime.UtcNow);

        Assert.Single(result);
        Assert.Equal("TestSong", result.First().Title);
    }

    [Fact]
    public async Task GetPlaylistTracksSnapshotAsync_ShouldReturnEmpty_WhenResponseInvalid()
    {
        var response = MakeResponse(default);
        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response);

        var result = await _provider.GetPlaylistTracksSnapshotAsync("pid", "spotify", DateTime.UtcNow);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteSafeAsync_ShouldThrow_WhenRateLimitExceeded()
    {
        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ThrowsAsync(new InvalidOperationException("limit exceeded"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _provider.GetPlaylistMetadataAsync("p1", "spotify"));
    }

    [Fact]
    public async Task ExecuteSafeAsync_ShouldReturnNull_WhenApiFails()
    {
        var response = MakeResponse(default, success: false);
        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response);

        var result = await _provider.GetPlaylistMetadataAsync("p1", "spotify");
        Assert.Null(result);
    }
}
