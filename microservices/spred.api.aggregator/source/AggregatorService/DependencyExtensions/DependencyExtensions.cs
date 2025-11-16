using AggregatorService.Abstractions;
using Polly;
using Refit;

namespace AggregatorService.DependencyExtensions;

public static class DependencyExtensions
{
    public static void AddRestServices(this IServiceCollection services)
    {
        services
            .AddRefitClient<IChartmetricsApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri("https://api.chartmetric.com");
                c.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler(
                Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(1)));
        
        services
            .AddRefitClient<ISpotifyApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri("https://api.spotify.com/v1");
                c.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler(
                Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(1)));
        
        services
            .AddRefitClient<ISpotifyAuthApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri("https://accounts.spotify.com");
                c.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler(
                Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(1)));
        
        services
            .AddRefitClient<ISoundchartsApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri("https://customer.api.soundcharts.com");
                c.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler(
                Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(1)));
    }
}