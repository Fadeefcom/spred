using System.Reflection;
using System.Text.Json;
using AggregatorService.Components;
using Microsoft.Extensions.Logging;
using Moq;
using Refit;
using StackExchange.Redis;

namespace AggregatorService.Test;

public class ApiRateLimiterTests
{
    private readonly Mock<ILogger<BaseApiRateLimiter>> _loggerMock = new();
    private readonly Mock<IApiResponse<JsonElement>> _responseMock = new();

    private BaseApiRateLimiter CreateLimiter(string servicePrefix)
    {
        return servicePrefix switch
        {
            "chartmetrics" => new ChartmetricsRateLimiter(Mock.Of<ILogger<ChartmetricsRateLimiter>>(), Mock.Of<IConnectionMultiplexer>()),
            _ => new SoundchartsRateLimiter(Mock.Of<ILogger<SoundchartsRateLimiter>>(), Mock.Of<IConnectionMultiplexer>())
        };
    }

    [Theory]
    [InlineData("chartmetrics")]
    [InlineData("soundcharts")]
    public async Task ExecuteAsync_ShouldReturnResponse_WhenWithinLimit(string servicePrefix)
    {
        // Arrange
        var limiter = CreateLimiter(servicePrefix);

        // Устанавливаем внутреннее поле "_remaining"
        typeof(BaseApiRateLimiter)
            .GetField("_remaining", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(limiter, 5);

        _responseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        _responseMock.Setup(r => r.Content).Returns(JsonDocument.Parse("{}").RootElement);
        _responseMock.Setup(r => r.Headers).Returns(new HttpResponseMessage().Headers);

        // Act
        var result = await limiter.ExecuteAsync(() => Task.FromResult(_responseMock.Object));

        // Assert
        Assert.True(result.IsSuccessStatusCode);

        var remaining = (int)typeof(BaseApiRateLimiter)
            .GetField("_remaining", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(limiter)!;

        Assert.Equal(limiter.DefaultLimit - 1, remaining); // Один запрос должен был "съесть" слот
    }

    [Theory]
    [InlineData("chartmetrics")]
    [InlineData("soundcharts")]
    public async Task ExecuteAsync_ShouldResetWindow_WhenExpired(string servicePrefix)
    {
        // Arrange
        var limiter = CreateLimiter(servicePrefix);

        typeof(BaseApiRateLimiter)
            .GetField("_remaining", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(limiter, 0);

        typeof(BaseApiRateLimiter)
            .GetField("_resetAt", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(limiter, DateTimeOffset.UtcNow.AddSeconds(-1)); // уже истёк

        _responseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        _responseMock.Setup(r => r.Content).Returns(JsonDocument.Parse("{}").RootElement);
        _responseMock.Setup(r => r.Headers).Returns(new HttpResponseMessage().Headers);

        // Act
        await limiter.ExecuteAsync(() => Task.FromResult(_responseMock.Object));

        // Assert
        var remaining = (int)typeof(BaseApiRateLimiter)
            .GetField("_remaining", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(limiter)!;

        Assert.True(remaining < limiter.DefaultLimit); // лимит должен был сброситься и уменьшиться на 1
    }

    [Theory]
    [InlineData("chartmetrics")]
    [InlineData("soundcharts")]
    public async Task UpdateFromHeadersAsync_ShouldUpdateRemaining_AndResetAt(string servicePrefix)
    {
        // Arrange
        var limiter = CreateLimiter(servicePrefix);

        var headers = new HttpResponseMessage().Headers;
        headers.Add("X-RateLimit-Remaining", "42");
        headers.Add("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeSeconds().ToString());

        // Act
        await limiter.UpdateFromHeadersAsync(headers);

        // Assert
        var remaining = (int)typeof(BaseApiRateLimiter)
            .GetField("_remaining", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(limiter)!;

        var resetAt = (DateTimeOffset)typeof(BaseApiRateLimiter)
            .GetField("_resetAt", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(limiter)!;

        Assert.Equal(42, remaining);
        Assert.True(resetAt > DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData("chartmetrics")]
    [InlineData("soundcharts")]
    public async Task ExecuteAsync_ShouldWaitAndProceed_WhenQuotaExceeded(string servicePrefix)
    {
        // Arrange
        var limiter = CreateLimiter(servicePrefix);

        typeof(BaseApiRateLimiter)
            .GetField("_remaining", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(limiter, 0);

        // Устанавливаем resetAt на 1 секунду вперёд — значит, должен подождать
        typeof(BaseApiRateLimiter)
            .GetField("_resetAt", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(limiter, DateTimeOffset.UtcNow.AddSeconds(1));

        _responseMock.Setup(r => r.IsSuccessStatusCode).Returns(true);
        _responseMock.Setup(r => r.Content).Returns(JsonDocument.Parse("{}").RootElement);
        _responseMock.Setup(r => r.Headers).Returns(new HttpResponseMessage().Headers);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await limiter.ExecuteAsync(() => Task.FromResult(_responseMock.Object));

        sw.Stop();

        // Assert — должно занять примерно >= 1 секунды (ожидание окна)
        Assert.True(sw.ElapsedMilliseconds >= 900, $"Elapsed {sw.ElapsedMilliseconds}ms — ожидание не сработало");
    }
}
