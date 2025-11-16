using System.Text.Json;
using AggregatorService.Abstractions;
using AggregatorService.Configurations;
using AggregatorService.Extensions;
using AggregatorService.Models.Dto;
using Extensions.Extensions;
using Microsoft.Extensions.Options;
using Refit;

namespace AggregatorService.Components;

/// <inheritdoc cref="IChartmetricsTokenProvider" />
public class ChartmetricsTokenProvider : IChartmetricsTokenProvider, IDisposable
{
    private string _accessToken = string.Empty;
    private DateTime _expires = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<ChartmetricsTokenProvider> _logger;
    private readonly IChartmetricsApi _chartmetricsApi;
    private readonly string _refreshToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartmetricsTokenProvider"/> class
    /// for managing and caching access tokens used to authenticate Chartmetrics API calls.
    /// </summary>
    /// <param name="logger">Logger instance used for diagnostics and error reporting.</param>
    /// <param name="chartmetricOptions">The application configuration options containing the Chartmetrics refresh token.</param>
    public ChartmetricsTokenProvider(ILogger<ChartmetricsTokenProvider> logger, IOptions<ChartmetricOptions> chartmetricOptions)
    {
        _logger = logger;
        _chartmetricsApi = RestService.For<IChartmetricsApi>("https://api.chartmetric.com");
        _refreshToken = chartmetricOptions.Value.RefreshToken;
    }

    /// <inheritdoc />
    public async Task<string> GetAccessTokenAsync()
    {
        if (_expires > DateTime.UtcNow)
            return _accessToken;

        await _lock.WaitAsync();
        try
        {
            if (_expires > DateTime.UtcNow)
                return _accessToken;

            var newToken = await RequestNewTokenAsync();
            _accessToken = newToken.AccessToken;
            _expires = DateTime.UtcNow.AddSeconds(newToken.ExpiresIn - 60);

            return _accessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<(string AccessToken, int ExpiresIn)> RequestNewTokenAsync()
    {
        var request = new ChartTokenRequest
        {
            RefreshToken = _refreshToken
        };

        try
        {
            var response = await _chartmetricsApi.GetAccessToken(request);

            if (response.IsSuccessful && response.Content.ValueKind == JsonValueKind.Object)
            {
                var token = response.Content.TryGetValue("token").GetStringOrNull();
                var expiresInElement = response.Content.TryGetValue("expires_in");

                if (!string.IsNullOrWhiteSpace(token) && 
                    expiresInElement?.ValueKind == JsonValueKind.Number && 
                    expiresInElement.Value.TryGetInt32(out var expiresIn))
                {
                    _logger.LogSpredInformation("ChartmetricsAccessToken",
                        "Chartmetric access token obtained successfully.");
                    return (token, expiresIn);
                }
            }

            _logger.LogSpredWarning("ChartmetricsAccessToken", $"Failed to obtain access token. StatusCode: {response.StatusCode}, Reason: {response.ReasonPhrase}");
            throw new InvalidOperationException("Failed to obtain Chartmetric access token.");
        }
        catch (System.Exception ex)
        {
            _logger.LogSpredWarning("ChartmetricsAccessToken", $"Exception while requesting token: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}