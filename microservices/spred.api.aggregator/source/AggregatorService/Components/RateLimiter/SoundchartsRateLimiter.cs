using StackExchange.Redis;

namespace AggregatorService.Components;

/// <summary>
/// Provides a rate-limiting mechanism specifically for the Soundcharts API.
/// Inherits core functionality from <see cref="BaseApiRateLimiter"/> and defines
/// Soundcharts-specific configuration such as Redis key prefix and default request limit.
/// </summary>
public sealed class SoundchartsRateLimiter : BaseApiRateLimiter
{
    /// <summary>
    /// Gets the Redis key prefix used to store rate-limiting state for Soundcharts API requests.
    /// </summary>
    protected override string ServicePrefix => "soundcharts";

    /// <summary>
    /// Gets the default daily rate limit value for the Soundcharts API.
    /// </summary>
    protected internal override int DefaultLimit => 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoundchartsRateLimiter"/> class.
    /// </summary>
    /// <param name="logger">The logger used for diagnostic and rate-limit tracking messages.</param>
    /// <param name="redis">The Redis connection used to persist rate-limit state across service instances.</param>
    public SoundchartsRateLimiter(ILogger<SoundchartsRateLimiter> logger, IConnectionMultiplexer redis)
        : base(logger, redis)
    {
    }
}