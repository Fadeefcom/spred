using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AggregatorService.Abstractions;
using Extensions.Extensions;
using Refit;
using StackExchange.Redis;

namespace AggregatorService.Components;

/// <summary>
/// Provides a generic base implementation for distributed API rate limiters.
/// This class manages rate-limit state persistence via Redis, enforces execution locks,
/// and automatically updates quota metadata from API response headers.
/// </summary>
public abstract class BaseApiRateLimiter : IApiRateLimiter, IDisposable
{
    private readonly ILogger _logger;
    private readonly IConnectionMultiplexer _redis;
    private int _remaining;
    private DateTimeOffset _resetAt = DateTimeOffset.UtcNow;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Gets the unique prefix for Redis keys associated with the specific API service.
    /// Derived classes must override this property to identify their service context.
    /// </summary>
    protected abstract string ServicePrefix { get; }

    /// <summary>
    /// Gets the default daily rate limit value for the target API.
    /// Can be overridden by derived implementations to customize service-specific limits.
    /// </summary>
    protected internal virtual int DefaultLimit => 60;

    private string RemainingKey => $"{ServicePrefix}:rate:remaining";
    private string ResetKey => $"{ServicePrefix}:rate:reset";
    private string UpdatedKey => $"{ServicePrefix}:rate:updated";

    /// <summary>
    /// Gets the number of remaining requests available in the current rate window.
    /// </summary>
    public int Remaining => _remaining;

    /// <summary>
    /// Gets the UTC timestamp at which the current rate limit window resets.
    /// </summary>
    public DateTimeOffset ResetAt => _resetAt;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseApiRateLimiter"/> class.
    /// </summary>
    /// <param name="logger">The logger used for diagnostic and monitoring output.</param>
    /// <param name="redis">The Redis connection used to persist and synchronize rate-limit state across instances.</param>
    protected BaseApiRateLimiter(ILogger logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
        LoadFromCacheAsync().AsTask().ConfigureAwait(false);
    }
    
    /// <summary>
    /// Resets the rate limit window for the next minute.
    /// </summary>
    private void ResetWindow()
    {
        _remaining = DefaultLimit;
        _resetAt = DateTimeOffset.UtcNow.AddMinutes(1);

        _logger.LogSpredDebug($"{ServicePrefix}RateLimiterReset",
            $"New window: remaining={_remaining}, resetAt={_resetAt:O}");
    }
    
    /// <summary>
    /// Acquires one quota slot for this minute.
    /// If the quota is exhausted, waits until the next window reset.
    /// </summary>
    private async Task AcquireSlotAsync()
    {
        while (true)
        {
            await _semaphore.WaitAsync();

            try
            {
                var now = DateTimeOffset.UtcNow;

                if (now >= _resetAt)
                    ResetWindow();

                if (_remaining > 0)
                {
                    _remaining--;
                    return;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            var waitTime = Math.Max(100, (_resetAt - DateTimeOffset.UtcNow).TotalMilliseconds);
            _logger.LogSpredInformation($"{ServicePrefix}RateLimiterWait",
                $"Rate limit reached. Waiting {waitTime / 1000:F1}s until {_resetAt:O}");
            await Task.Delay(TimeSpan.FromMilliseconds(waitTime));
        }
    }

    /// <summary>
    /// Loads cached rate-limit data from Redis if available.
    /// Initializes the in-memory state with the last known remaining quota and reset timestamp.
    /// </summary>
    private async ValueTask LoadFromCacheAsync()
    {
        var db = _redis.GetDatabase();
        var remTask = db.StringGetAsync(RemainingKey);
        var resetTask = db.StringGetAsync(ResetKey);

        await Task.WhenAll(remTask, resetTask).ConfigureAwait(false);

        if (int.TryParse(remTask.Result, out var rem))
            _remaining = rem;
        else
            _remaining = DefaultLimit;

        if (long.TryParse(resetTask.Result, out var resetEpoch))
            _resetAt = DateTimeOffset.FromUnixTimeSeconds(resetEpoch);

        _logger.LogSpredInformation($"{ServicePrefix}RateLimiterInit",
            $"Init complete: remaining={_remaining}, resetAt={_resetAt:O}");
    }

    /// <summary>
    /// Determines whether a batch of requests can be executed without exceeding the rate limit.
    /// </summary>
    /// <param name="expectedCount">The number of expected requests in the batch.</param>
    /// <returns><c>true</c> if sufficient quota is available or if the reset window has passed; otherwise, <c>false</c>.</returns>
    public bool CanExecuteBatch(int expectedCount)
    {
        var now = DateTimeOffset.UtcNow;
        return _remaining >= expectedCount || now >= _resetAt;
    }


    /// <summary>
    /// Executes a rate-limited API request while ensuring concurrency safety and compliance with quota restrictions.
    /// If the limit is exceeded before the reset window, an <see cref="InvalidOperationException"/> is thrown.
    /// </summary>
    /// <param name="apiCall">A delegate representing the asynchronous API call to be executed.</param>
    /// <returns>The API response object.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the rate limit is reached before the reset period expires.</exception>
    public async Task<IApiResponse<JsonElement>> ExecuteAsync(Func<Task<IApiResponse<JsonElement>>> apiCall)
    {
        await AcquireSlotAsync();

        var response = await apiCall().ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            await UpdateFromHeadersAsync(response.Headers);
        }

        return response;
    }

    /// <summary>
    /// Updates quota values from HTTP headers if provided by the API.
    /// Recognizes X-RateLimit-Remaining and X-RateLimit-Reset.
    /// </summary>
    public async Task UpdateFromHeadersAsync(HttpHeaders? headers)
    {
        if (headers == null) return;

        int? newRemaining = null;
        long? newResetEpoch = null;

        if (headers.TryGetValues("X-RateLimit-Remaining", out var remValues) &&
            int.TryParse(remValues.FirstOrDefault(), out var remaining))
            newRemaining = remaining;

        if (headers.TryGetValues("X-RateLimit-Reset", out var resetValues) &&
            long.TryParse(resetValues.FirstOrDefault(), out var resetEpoch))
            newResetEpoch = resetEpoch;

        await _semaphore.WaitAsync();
        try
        {
            if (newRemaining.HasValue)
                _remaining = newRemaining.Value;

            if (newResetEpoch.HasValue)
                _resetAt = DateTimeOffset.FromUnixTimeSeconds(newResetEpoch.Value);

            _logger.LogSpredDebug($"{ServicePrefix}RateLimiter429Update",
                $"Updated from headers: remaining={_remaining}, resetAt={_resetAt:O}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
