using Extensions.DiExtensions;
using Extensions.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Refit;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Repositories;
using StackExchange.Redis;
using SubmissionService.Abstractions;
using SubmissionService.Models.Entities;

namespace SubmissionService.DependencyExtensions;

/// <summary>
/// Provides extension methods for configuring dependency injection
/// of application and external services in the Submission Service.
/// </summary>
public static class DependencyExtension
{
    /// <summary>
    /// Registers core application services, including Cosmos DB containers
    /// and persistence stores for submission-related entities.
    /// </summary>
    /// <param name="services">The service collection to which the services are added.</param>
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddCosmosClient();
        var composite = new List<CompositePath>();
        string[][] uniqueIndex =
        [
            ["/CatalogItemId", "/Type", "/TrackId"]
        ];
        var excludedPaths = new List<ExcludedPath>
        {
            new() { Path = "/Payload/*" }
        };
        
        services.AddContainer<Submission>(composite, uniqueKeys: uniqueIndex, excludedPaths: excludedPaths, version: 1, containerName: "Submissions");
        services.AddContainer<OutboxEvent>(composite, version: 1, excludedPaths: excludedPaths, containerName: "Submissions");
        services.AddContainer<ArtistInbox>(composite, uniqueKeys: [ ["/CatalogItemId", "/TrackId"] ], version: 1);
        
        services.AddScoped<IPersistenceStore<Submission, Guid>, PersistenceStore<Submission, Guid>>();
        services.AddScoped<IPersistenceStore<OutboxEvent, Guid>, PersistenceStore<OutboxEvent, Guid>>();
        services.AddScoped<IPersistenceStore<ArtistInbox, Guid>, PersistenceStore<ArtistInbox, Guid>>();
        
        services.AddSingleton<IConnectionMultiplexer>((serviceProvider) =>
        {
            var redisOptions = serviceProvider.GetRequiredService<IOptions<RedisOptions>>();
            var options = ConfigurationOptions.Parse(redisOptions.Value.ConnectionString);
            return ConnectionMultiplexer.Connect(options);
        });
    }

    /// <summary>
    /// Registers external services such as the track and catalog service clients,
    /// using Refit and configured with retry policies.
    /// </summary>
    /// <param name="services">The service collection to which the services are added.</param>
    public static void AddExternalServices(this IServiceCollection services)
    {
        services
            .AddRefitClient<ITrackService>()
            .ConfigureHttpClient((p, c) =>
            {
                var options = p.GetRequiredService<IOptions<ServicesOuterOptions>>();
                c.BaseAddress = new Uri(options.Value.TrackService);
                c.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler(CorsPolicyExtensions.BuildRetryPolicy("retry:track"));
        
        services
            .AddRefitClient<ICatalogService>()
            .ConfigureHttpClient((p, c) =>
            {
                var options = p.GetRequiredService<IOptions<ServicesOuterOptions>>();
                c.BaseAddress = new Uri(options.Value.PlaylistService);
                c.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler(CorsPolicyExtensions.BuildRetryPolicy("retry:catalog"));
    }
}
