using AggregatorService.Abstractions;
using AggregatorService.DependencyExtensions;
using Microsoft.Extensions.DependencyInjection;

namespace AggregatorService.Test;

public class DependencyExtensionsTest
{
    [Fact]
    public void AddRestServices_ShouldRegisterClients()
    {
        var services = new ServiceCollection();
        services.AddRestServices();
        var provider = services.BuildServiceProvider();

        var chartApi = provider.GetService<IChartmetricsApi>();
        var spotifyApi = provider.GetService<ISpotifyApi>();
        var spotifyAuthApi = provider.GetService<ISpotifyAuthApi>();

        Assert.NotNull(chartApi);
        Assert.NotNull(spotifyApi);
        Assert.NotNull(spotifyAuthApi);
    }
}