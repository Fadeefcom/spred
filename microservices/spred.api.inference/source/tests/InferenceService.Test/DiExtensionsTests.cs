using Extensions.Models;
using InferenceService.DependencyExtensions;
using InferenceService.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Repository.Abstractions.Interfaces;
using StackExchange.Redis;

namespace InferenceService.Test;

public class DiExtensionsSmokeTests
{
    private ServicesOuterOptions CreateOptions() =>
        new ServicesOuterOptions
        {
            AggregatorService = "http://localhost",
            TrackService = "http://localhost",
            AuthorizationService = "http://localhost",
            InferenceService = "http://localhost",
            PlaylistService = "http://localhost",
            UiEndpoint = "http://localhost",
            VectorService = "http://localhost",
            SubscriptionService = "http://localhost"
        };

    [Fact]
    public void AddAppServices_ShouldExecute()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<ServicesOuterOptions>>(Options.Create(CreateOptions()));

        services.AddAppServices();
    }

    [Fact]
    public void AddApplicationStores_ShouldExecute()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<RedisOptions>>(Options.Create(new RedisOptions
        {
            ConnectionString = "localhost:6379",
            InstanceName = "Test"
        }));

        services.AddSingleton<IPersistenceStore<InferenceResult, Guid>>(
            new Mock<IPersistenceStore<InferenceResult, Guid>>().Object);
        services.AddSingleton<IConnectionMultiplexer>(
            new Mock<IConnectionMultiplexer>().Object);

        services.AddApplicationStores();
    }
}