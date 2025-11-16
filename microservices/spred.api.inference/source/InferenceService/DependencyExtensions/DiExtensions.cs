using System.Diagnostics.CodeAnalysis;
using Extensions.DiExtensions;
using Extensions.Models;
using InferenceService.Abstractions;
using InferenceService.Components;
using InferenceService.Models.Entities;
using Microsoft.Extensions.Options;
using Refit;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Repositories;
using StackExchange.Redis;

namespace InferenceService.DependencyExtensions;

/// <summary>
/// Provides extension methods for dependency injection.
/// </summary>
[ExcludeFromCodeCoverage]
public static class DiExtensions
{
    /// <summary>
    /// Adds application stores and repositories to the service collection.
    /// </summary>
    /// <param name="serviceCollection">The service collection to add the stores to.</param>
    /// <param name="isProduction"></param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddApplicationStores(this IServiceCollection serviceCollection,
        bool isProduction = false)
    {
        serviceCollection.AddCosmosClient();
        serviceCollection.AddContainer<InferenceResult>([]);
        serviceCollection.AddScoped<IInferenceManager, InferenceManager>();
         
        serviceCollection.AddScoped<IPersistenceStore<InferenceResult, Guid>, PersistenceStore<InferenceResult, Guid>>();
        serviceCollection.AddSingleton<IConnectionMultiplexer>((serviceProvider) =>
        {
            var redisOptions = serviceProvider.GetRequiredService<IOptions<RedisOptions>>();
            var options = ConfigurationOptions.Parse(redisOptions.Value.ConnectionString);
            return ConnectionMultiplexer.Connect(options);
        });

        return serviceCollection;
    }

    /// <summary>
    /// Registers Refit HTTP clients with retry policies, timeouts, and base addresses
    /// configured from <see cref="ServicesOuterOptions"/>.
    /// Adds IMemoryCache for local in-memory retry tracking, 
    /// and limits retries to 20% of all requests per service instance.
    /// </summary>
    /// <param name="serviceCollection">The service collection to add the clients to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAppServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddMemoryCache();
        
        serviceCollection.AddRefitClient<IVectorSearch>()
            .ConfigureHttpClient((sp, c) =>
            {
                var options = sp.GetRequiredService<IOptions<ServicesOuterOptions>>().Value;
                c.BaseAddress = new Uri(options.VectorService);
                c.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler(CorsPolicyExtensions.BuildRetryPolicy("retry:vector"));

        serviceCollection.AddRefitClient<ITrackServiceApi>()
            .ConfigureHttpClient((sp, c) =>
            {
                var options = sp.GetRequiredService<IOptions<ServicesOuterOptions>>().Value;
                c.BaseAddress = new Uri(options.TrackService);
                c.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler(CorsPolicyExtensions.BuildRetryPolicy("retry:track"));

        serviceCollection.AddRefitClient<IPlaylistServiceApi>()
            .ConfigureHttpClient((sp, c) =>
            {
                var options = sp.GetRequiredService<IOptions<ServicesOuterOptions>>().Value;
                c.BaseAddress = new Uri(options.PlaylistService);
                c.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler(CorsPolicyExtensions.BuildRetryPolicy("retry:playlist"));

        return serviceCollection;
    }
}
