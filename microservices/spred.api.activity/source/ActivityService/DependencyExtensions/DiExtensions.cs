using ActivityService.Abstractions;
using ActivityService.Components.Services;
using ActivityService.Models;
using Extensions.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Repositories;
using StackExchange.Redis;

namespace ActivityService.DependencyExtensions;

/// <summary>
/// Provides extension methods for dependency injection in the application.
/// </summary>
public static class DiExtensions
{
    /// <summary>
    /// Registers the application-specific stores and related services with the provided service collection.
    /// </summary>
    /// <param name="serviceCollection">
    /// The service collection to which the application-specific dependencies will be added.
    /// </param>
    public static void AddApplicationStores(this IServiceCollection serviceCollection)
    {
        var excludedPaths = new List<ExcludedPath>
        {
            new() { Path = "/Args/*" },
            new() { Path = "/Before/*" },
            new() { Path = "/After/*" }
        };

        serviceCollection.AddCosmosClient();
        serviceCollection.AddContainer<ActivityEntity>(
            index: [],
            excludedPaths: excludedPaths,
            uniqueKeys:
            [
                ["/Sequence"]
            ]
        );
        
        serviceCollection.AddScoped<IPersistenceStore<ActivityEntity, Guid>, PersistenceStore<ActivityEntity, Guid>>();
        serviceCollection.AddSingleton<IActivityMessageFormatter, ActivityMessageFormatter>();
        serviceCollection.AddSingleton<IConnectionMultiplexer>((serviceProvider) =>
        {
            var redisOptions = serviceProvider.GetRequiredService<IOptions<RedisOptions>>();
            var options = ConfigurationOptions.Parse(redisOptions.Value.ConnectionString);
            return ConnectionMultiplexer.Connect(options);
        });
    }
}