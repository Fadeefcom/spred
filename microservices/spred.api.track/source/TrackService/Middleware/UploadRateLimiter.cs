using System.Globalization;
using System.Security.Claims;
using Extensions.Extensions;
using StackExchange.Redis;

namespace TrackService.Middleware;

/// <summary>
/// Endpoint filter that enforces upload rate limiting for free tier users.
/// </summary>
/// <remarks>
/// This filter restricts free tier users to a maximum of 3 track uploads per week.
/// Premium users are exempt from rate limiting. The rate limit state is stored in Redis
/// with a 7-day expiration period from the first upload request.
/// </remarks>
public class UploadRateLimiter : IEndpointFilter
{
    private readonly IDatabase _redis;

    /// <summary>
    /// The maximum number of tracks allowed for free tier users within the rate limit window.
    /// </summary>
    public const int FreeTierTrackLimit = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadRateLimiter"/> class.
    /// </summary>
    /// <param name="connectionMultiplexer">The Redis connection multiplexer used to access rate limit data.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionMultiplexer"/> is null.</exception>
    public UploadRateLimiter(IConnectionMultiplexer connectionMultiplexer)
    {
        _redis = connectionMultiplexer.GetDatabase();
    }

    /// <summary>
    /// Invokes the rate limiting filter to enforce upload restrictions for free tier users.
    /// </summary>
    /// <param name="context">The endpoint filter invocation context containing HTTP context and arguments.</param>
    /// <param name="next">The delegate representing the next filter in the pipeline.</param>
    /// <returns>
    /// Returns <see cref="Results.Unauthorized"/> if user is not authenticated,
    /// <see cref="Results.BadRequest"/> if free tier user exceeds upload limit,
    /// or the result of the next filter in the pipeline if rate limiting passes.
    /// </returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        if (!user.IsPremium())
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var redisKey = $"track:limit:{userId}";
            var lockKey = $"track:lock:{userId}";
            var ttlKey = TimeSpan.FromDays(7);

            var acquired = await _redis.StringSetAsync(lockKey, "1", TimeSpan.FromMinutes(5), when: When.NotExists);
            if (!acquired)
                return Results.BadRequest("You already have a track being processed. Please wait until it finishes.");

            try
            {
                var result = await next(context);

                if (result is IResult && httpContext.Response.StatusCode is >= 200 and < 300)
                {
                    var trackCount = await _redis.StringIncrementAsync(redisKey);
                    if (trackCount == 1)
                        await _redis.KeyExpireAsync(redisKey, ttlKey);

                    var ttl = await _redis.KeyTimeToLiveAsync(redisKey);
                    var resetTime = DateTimeOffset.UtcNow.Add(ttl ?? ttlKey);
                    var resetUnix = resetTime.ToUnixTimeSeconds();

                    httpContext.Response.Headers["X-RateLimit-Limit"] =
                        FreeTierTrackLimit.ToString(CultureInfo.InvariantCulture);
                    httpContext.Response.Headers["X-RateLimit-Used"] = Math.Min(trackCount, FreeTierTrackLimit)
                        .ToString(CultureInfo.InvariantCulture);
                    httpContext.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, FreeTierTrackLimit - trackCount)
                        .ToString(CultureInfo.InvariantCulture);
                    httpContext.Response.Headers["X-RateLimit-Reset"] =
                        resetUnix.ToString(CultureInfo.InvariantCulture);

                    if (trackCount > FreeTierTrackLimit)
                        return Results.BadRequest("Free plan allows up to 3 tracks per week. Upgrade to add more.");
                }

                return result;
            }
            finally
            {
                await _redis.KeyDeleteAsync(lockKey);
            }
        }
        else
        {
            httpContext.Response.Headers["X-RateLimit-Limit"] = "unlimited";
            httpContext.Response.Headers["X-RateLimit-Used"] = "0";
            httpContext.Response.Headers["X-RateLimit-Remaining"] = "unlimited";
            httpContext.Response.Headers["X-RateLimit-Reset"] = "0";
        }

        return await next(context);
    }
}