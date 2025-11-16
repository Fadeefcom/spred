namespace AggregatorService.Abstractions;

/// <summary>
/// Provides access tokens for authenticating requests to the Chartmetrics API.
/// </summary>
public interface IChartmetricsTokenProvider
{
    /// <summary>
    /// Asynchronously retrieves a valid access token for use with the Chartmetrics API.
    /// </summary>
    /// <returns>A task that resolves to a Bearer token string.</returns>
    Task<string> GetAccessTokenAsync();
}