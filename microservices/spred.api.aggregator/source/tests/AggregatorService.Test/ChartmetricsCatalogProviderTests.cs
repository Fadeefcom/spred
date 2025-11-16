using System.Text.Json;
using AggregatorService.Abstractions;
using AggregatorService.Components;
using AggregatorService.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Refit;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;

namespace AggregatorService.Test;

public class ChartmetricsCatalogProviderTests
{
    private readonly Mock<IChartmetricsApi> _apiMock = new();
    private readonly Mock<IChartmetricsTokenProvider> _tokenProviderMock = new();
    private readonly Mock<IApiRateLimiter> _rateLimiterMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ILogger<ChartmetricsCatalogProvider>> _loggerMock = new();
    private readonly ChartmetricsCatalogProvider _provider;

    public ChartmetricsCatalogProviderTests()
    {
        _provider = new ChartmetricsCatalogProvider(
            _apiMock.Object,
            _tokenProviderMock.Object,
            _rateLimiterMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    private static IApiResponse<JsonElement> MakeResponse(JsonElement element, bool success = true)
    {
        var mock = new Mock<IApiResponse<JsonElement>>();
        mock.Setup(r => r.IsSuccessStatusCode).Returns(success);
        mock.Setup(r => r.Content).Returns(element);
        return mock.Object;
    }

    [Fact]
    public async Task ResolvePlaylistIdAsync_ShouldReturnId_WhenResponseValid()
    {
        // Arrange
        _tokenProviderMock.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("token");
        var json = JsonDocument.Parse(@"{ ""obj"": { ""playlists"": { ""spotify"": [ { ""id"": 12345 } ] } } }");
        var response = MakeResponse(json.RootElement);
        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response);

        // Act
        var result = await _provider.ResolvePlaylistIdAsync("spotify:playlist:abc", "spotify");

        // Assert
        Assert.Equal("12345", result);
    }

    [Fact]
    public async Task ResolvePlaylistIdAsync_ShouldReturnNull_WhenResponseInvalid()
    {
        _tokenProviderMock.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("token");
        var response = MakeResponse(default, success: false);
        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response);

        var result = await _provider.ResolvePlaylistIdAsync("spotify:playlist:abc", "spotify");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPlaylistMetadataAsync_ShouldMapMetadata_WhenResponseValid()
    {
        // Arrange
        _tokenProviderMock.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("token");
        var json = JsonDocument.Parse(@"{ ""obj"": { ""id"": ""abc"", ""name"": ""Playlist"" } }");
        var response = MakeResponse(json.RootElement);
        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response);
        _mapperMock.Setup(m => m.Map<MetadataDto>(It.IsAny<JsonElement>())).Returns(new MetadataDto
        {
            Name = "Playlist",
            PrimaryId = "Test"
        });

        // Act
        var result = await _provider.GetPlaylistMetadataAsync("id1", "spotify");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Playlist", result!.Name);
    }

    [Fact]
    public async Task GetPlaylistMetadataAsync_ShouldReturnNull_WhenContentUndefined()
    {
        _tokenProviderMock.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("token");
        var response = MakeResponse(default);
        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response);

        var result = await _provider.GetPlaylistMetadataAsync("id1", "spotify");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPlaylistStatsAsync_ShouldReturnMappedStats_WhenValid()
    {
        _tokenProviderMock.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("token");
        var json = JsonDocument.Parse(@"{ ""obj"": [ { ""date"": ""2024-10-01T00:00:00Z"", ""value"": 42 } ] }");
        var response = MakeResponse(json.RootElement);
        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response);

        var expected = new HashSet<StatInfo> { new() { Value = 42, Timestamp = DateTime.Parse("2024-10-01") } };
        _mapperMock.Setup(m => m.Map<HashSet<StatInfo>>(It.IsAny<List<JsonElement>>())).Returns(expected);

        var result = await _provider.GetPlaylistStatsAsync("id1", "spotify", true);

        Assert.Single(result);
        Assert.Equal(42u, result.First().Value);
    }

    [Fact]
    public async Task GetPlaylistStatsAsync_ShouldReturnEmpty_WhenUpdateStatsFalse()
    {
        var result = await _provider.GetPlaylistStatsAsync("id1", "spotify", false);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPlaylistTracksSnapshotAsync_ShouldReturnMappedTracks_WhenResponseValid()
    {
        _tokenProviderMock.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("token");
        var json = JsonDocument.Parse(@"{ ""obj"": [ { ""id"": 1 }, { ""id"": 2 } ] }");
        var response = MakeResponse(json.RootElement);
        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response);

        var mapped = new List<TrackDtoWithPlatformIds> { new()
        {
            Title = "Test"
        }, new()
        {
            Title = "Test"
        }};
        _mapperMock.Setup(m => m.Map<List<TrackDtoWithPlatformIds>>(It.IsAny<List<ChartmetricsTrackWrapper>>()))
            .Returns(mapped);

        var result = await _provider.GetPlaylistTracksSnapshotAsync("id", "spotify", DateTime.UtcNow);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetPlaylistTracksSnapshotAsync_ShouldReturnEmpty_WhenInvalidJson()
    {
        _tokenProviderMock.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("token");
        var response = MakeResponse(default);
        _rateLimiterMock.Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ReturnsAsync(response);

        var result = await _provider.GetPlaylistTracksSnapshotAsync("id", "spotify", DateTime.UtcNow);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteSafeAsync_ShouldThrow_WhenRateLimitExceeded()
    {
        _tokenProviderMock.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("token");
        _rateLimiterMock
            .Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ThrowsAsync(new InvalidOperationException("Rate limit exceeded"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _provider.GetPlaylistMetadataAsync("id1", "spotify"));
    }

    [Fact]
    public async Task ExecuteSafeAsync_ShouldLogError_WhenUnexpectedException()
    {
        _tokenProviderMock.Setup(t => t.GetAccessTokenAsync()).ReturnsAsync("token");
        _rateLimiterMock
            .Setup(r => r.ExecuteAsync(It.IsAny<Func<Task<IApiResponse<JsonElement>>>>()))
            .ThrowsAsync(new System.Exception("unexpected"));

        var result = await _provider.GetPlaylistMetadataAsync("id1", "spotify");
        Assert.Null(result);
    }
}
