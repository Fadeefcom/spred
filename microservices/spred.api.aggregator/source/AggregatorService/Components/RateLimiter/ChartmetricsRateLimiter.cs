using StackExchange.Redis;

namespace AggregatorService.Components;

/// <summary>
/// Represents a rate limiter implementation for the Chartmetrics service.
/// </summary>
/// <remarks>
/// This class is a specialized implementation of the <see cref="BaseApiRateLimiter"/> for managing
/// API request limits specific to Chartmetrics. It enforces a default limit of 10,000 requests.
/// </remarks>
public sealed class ChartmetricsRateLimiter : BaseApiRateLimiter
{
    /// <summary>
    /// Gets the service-specific prefix used for constructing Redis keys or identifying
    /// the associated service in the rate-limiting context.
    /// </summary>
    /// <remarks>
    /// The <c>ServicePrefix</c> property is used to uniquely identify the service to which the rate limiter applies.
    /// Subclasses of <see cref="BaseApiRateLimiter"/> are required to define this property to differentiate themselves
    /// when managing API rate limits across multiple services.
    /// </remarks>
    protected override string ServicePrefix => "chartmetrics";

    /// <summary>
    /// Specifies the default rate limit for API requests in a derived implementation of <see cref="BaseApiRateLimiter"/>.
    /// </summary>
    /// <remarks>
    /// This property defines the maximum number of API requests that can be made within a specific time period for the associated service.
    /// The value is service-specific and is intended to enforce rate-limiting consistency across distributed systems.
    /// </remarks>
    protected internal override int DefaultLimit => 10000;

    /// A rate limiter implementation specifically tailored for managing API rate limits of the Chartmetrics service.
    /// It extends the abstract BaseApiRateLimiter class, allowing customization of rate limiting behavior
    /// for Chartmetrics-specific API usage scenarios.
    public ChartmetricsRateLimiter(ILogger<ChartmetricsRateLimiter> logger, IConnectionMultiplexer redis)
        : base(logger, redis)
    {
    }
}