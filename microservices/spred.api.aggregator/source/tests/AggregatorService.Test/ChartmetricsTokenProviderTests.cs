using System.Text.Json;
using AggregatorService.Abstractions;
using AggregatorService.Components;
using AggregatorService.Configurations;
using AggregatorService.Models.Dto;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Refit;

namespace AggregatorService.Test;

public class ChartmetricsTokenProviderTests
{
    private readonly Mock<IChartmetricsApi> _mockApi;
    private readonly ChartmetricsTokenProvider _provider;

    public ChartmetricsTokenProviderTests()
    {
        _mockApi = new Mock<IChartmetricsApi>();
        var mockLogger = new Mock<ILogger<ChartmetricsTokenProvider>>();

        var options = Options.Create(new ChartmetricOptions
        {
            RefreshToken = "test-refresh-token"
        });

        _provider = new ChartmetricsTokenProvider(mockLogger.Object, options);

        // hack: inject mocked API
        typeof(ChartmetricsTokenProvider)
            .GetField("_chartmetricsApi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(_provider, _mockApi.Object);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ReturnsToken_WhenApiReturnsValidResponse()
    {
        // Arrange
        var token = "abc123";
        var expiresIn = 300;

        var json = JsonDocument.Parse($$"""
        {
            "token": "{{token}}",
            "expires_in": {{expiresIn}}
        }
        """);

        var mockApiResponse = new Mock<IApiResponse<JsonElement>>();
        mockApiResponse.Setup(x => x.IsSuccessStatusCode).Returns(true);
        mockApiResponse.Setup(x => x.IsSuccessful).Returns(true);
        mockApiResponse.Setup(x => x.Content).Returns(json.RootElement);

        _mockApi
            .Setup(api => api.GetAccessToken(It.IsAny<ChartTokenRequest>()))
            .ReturnsAsync(mockApiResponse.Object);

        // Act
        var result = await _provider.GetAccessTokenAsync();

        // Assert
        result.Should().Be(token);

        // Call again: token should be reused (not re-fetched)
        var second = await _provider.GetAccessTokenAsync();
        second.Should().Be(token);

        _mockApi.Verify(api => api.GetAccessToken(It.IsAny<ChartTokenRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetAccessTokenAsync_Throws_WhenTokenInvalid()
    {
        // Arrange: missing "token" field
        var json = JsonDocument.Parse("""{ "expires_in": 300 }""");
        
        var mockApiResponse = new Mock<IApiResponse<JsonElement>>();
        mockApiResponse.Setup(x => x.IsSuccessStatusCode).Returns(true);
        mockApiResponse.Setup(x => x.Content).Returns(json.RootElement);

        _mockApi
            .Setup(api => api.GetAccessToken(It.IsAny<ChartTokenRequest>()))
            .ReturnsAsync(mockApiResponse.Object);

        typeof(ChartmetricsTokenProvider)
            .GetField("_chartmetricsApi", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(_provider, _mockApi.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _provider.GetAccessTokenAsync());
    }
    
    [Fact]
    public async Task GetAccessTokenAsync_Throws_WhenExpiresInInvalid()
    {
        var json = JsonDocument.Parse("""{ "token": "abc123", "expires_in": "not-a-number" }""");

        var mockApiResponse = new Mock<IApiResponse<JsonElement>>();
        mockApiResponse.Setup(x => x.IsSuccessStatusCode).Returns(true);
        mockApiResponse.Setup(x => x.IsSuccessful).Returns(true);
        mockApiResponse.Setup(x => x.Content).Returns(json.RootElement);

        _mockApi
            .Setup(api => api.GetAccessToken(It.IsAny<ChartTokenRequest>()))
            .ReturnsAsync(mockApiResponse.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _provider.GetAccessTokenAsync());
    }

    [Fact]
    public async Task GetAccessTokenAsync_Throws_WhenApiCallFails()
    {
        _mockApi
            .Setup(api => api.GetAccessToken(It.IsAny<ChartTokenRequest>()))
            .ThrowsAsync(new HttpRequestException("network error"));

        await Assert.ThrowsAsync<HttpRequestException>(() => _provider.GetAccessTokenAsync());
    }

    [Fact]
    public async Task GetAccessTokenAsync_RechecksExpirationInsideLock()
    {
        typeof(ChartmetricsTokenProvider)
            .GetField("_expires", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(_provider, DateTime.UtcNow.AddMinutes(5));

        var token = await _provider.GetAccessTokenAsync();

        token.Should().NotBeNull();
        _mockApi.Verify(api => api.GetAccessToken(It.IsAny<ChartTokenRequest>()), Times.Never);
    }

    [Fact]
    public async Task Dispose_DisposesSemaphore()
    {
        var semaphoreField = typeof(ChartmetricsTokenProvider)
            .GetField("_lock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(_provider) as SemaphoreSlim;

        _provider.Dispose();

        Func<Task> waitFunc = async () => await semaphoreField!.WaitAsync(1);
        await Assert.ThrowsAsync<ObjectDisposedException>(waitFunc);
    }
}