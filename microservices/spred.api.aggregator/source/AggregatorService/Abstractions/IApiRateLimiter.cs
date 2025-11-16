using System.Net.Http.Headers;
using System.Text.Json;
using Refit;

namespace AggregatorService.Abstractions;

/// <summary>
/// Defines a contract for implementing distributed API rate limiters.
/// </summary>
public interface IApiRateLimiter
{
    /// <summary>
    /// Gets the remaining request count for the current rate window.
    /// </summary>
    int Remaining { get; }

    /// <summary>
    /// Gets the reset time of the current rate window.
    /// </summary>
    DateTimeOffset ResetAt { get; }

    /// <summary>
    /// Determines whether a batch of requests can be executed without exceeding the limit.
    /// </summary>
    /// <param name="expectedCount">Number of expected requests.</param>
    /// <returns><c>true</c> if allowed, otherwise <c>false</c>.</returns>
    bool CanExecuteBatch(int expectedCount);

    /// <summary>
    /// Executes a rate-limited API call, throwing if the limit is reached.
    /// </summary>
    /// <param name="apiCall">Delegate representing the API call.</param>
    /// <returns>The API response.</returns>
    Task<IApiResponse<JsonElement>> ExecuteAsync(Func<Task<IApiResponse<JsonElement>>> apiCall);

    /// <summary>
    /// Updates the limiter state from HTTP response headers.
    /// </summary>
    /// <param name="headers">HTTP headers containing rate limit metadata.</param>
    Task UpdateFromHeadersAsync(HttpHeaders? headers);
}