using Extensions.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using PlaylistService.Abstractions;
using PlaylistService.Components.Services;
using PlaylistService.Models;
using PlaylistService.Models.Commands;
using PlaylistService.Models.Entities;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Repositories;
using StackExchange.Redis;

namespace PlaylistService.DependencyExtensions;

/// <summary>
/// Provides extension methods for dependency injection related to PlaylistService.
/// </summary>
public static class DiExtensions
{
    /// <summary>
    /// Adds the necessary services for the PlaylistService to the specified IServiceCollection.
    /// </summary>
    /// <param name="serviceCollection">The IServiceCollection to add services to.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddAppPlaylists(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddCosmosClient();
        serviceCollection.AddContainer<CatalogMetadata>([], uniqueKeys:[["/PrimaryId"]], version: 2);
        serviceCollection.AddContainer<CatalogStatistics>([]);

        // AddAsync event repository in pipeline
        serviceCollection.AddScoped<IManager, ManagerPlaylist>();
        serviceCollection.AddScoped<IPersistenceStore<CatalogMetadata, Guid>, PersistenceStore<CatalogMetadata, Guid>>();
        serviceCollection.AddScoped<IPersistenceStore<CatalogStatistics, Guid>, PersistenceStore<CatalogStatistics, Guid>>();
        serviceCollection.AddSingleton<IConnectionMultiplexer>((serviceProvider) =>
        {
            var redisOptions = serviceProvider.GetRequiredService<IOptions<RedisOptions>>();
            var options = ConfigurationOptions.Parse(redisOptions.Value.ConnectionString);
            return ConnectionMultiplexer.Connect(options);
        });
        

        return serviceCollection;
    }

    /// <summary>
    /// Add test playlist
    /// </summary>
    /// <param name="scope"></param>
    public static async Task InitTestPlaylistsAsync(this IServiceScope scope)
    {
        var ids = new List<Guid>() { Guid.Parse("07f7d778-5b74-4544-977a-01a11798e07d"), Guid.Parse("9713e536-73c7-4506-959d-120f35b8f42b") };
        var context = scope.ServiceProvider.GetRequiredService<IPersistenceStore<CatalogMetadata, Guid>>();
        var statStore = scope.ServiceProvider.GetRequiredService<IPersistenceStore<CatalogStatistics, Guid>>();

        foreach (var id in ids)
        {
            var playlist = new PlaylistMetadata();
            playlist.Create(new CreateMetadataCommand
            {
                Id = id,
                PrimaryId = PrimaryId.Parse("spotify:playlist:7ne0Wi3XxbqfTdvSQXKYRv"),
                SpredUserId = Guid.Empty,
                Name = "Lo-Fi Beats",
                Description = "Relax and study with this chill collection.",
                TracksTotal = 2,
                Followers = 1580,
                IsPublic = true,
                Collaborative = false,
                Tracks = [
                    Guid.Parse("95937e91-4baf-4c82-822e-f4ded431412e"), 
                    Guid.Parse("1eca47bb-92e9-4b71-a20d-0eeac231c245"), 
                    Guid.Parse("8a7e65d2-46f6-4df1-a4aa-a53b365034a5")],
                ListenUrls = new Dictionary<string, string>
            {
                { "spotify", "https://open.spotify.com/playlist/example123" },
                { "youtube", "https://youtube.com/playlist?list=xyz" }
            },
                SubmitUrls = new Dictionary<string, string>
            {
                { "form", "https://spred.fm/submit" }
            },
                ImageUrl = "https://spred.fm/images/playlist-cover.jpg",
                SubmitEmail = "submit@spred.fm",
                Type = "playlist"
            });
            playlist.Update(new UpdateMetadataCommand()
            {
                Id = id,
                PrimaryId = PrimaryId.Parse("spotify:playlist:example123"),
                Tags = ["Rock", "Music", "RnB"],
            });
            
            var stats = new List<CatalogStatistics>
            {
                new()
                {
                    Date = DateTime.UtcNow,
                    Followers = 1325,
                    FollowersDailyDiff = 24,
                    MetadataId = id
                },
                new()
                {
                    Date = DateTime.UtcNow.AddDays(-25),
                    Followers = 1200,
                    FollowersDailyDiff = 15,
                    MetadataId = id
                }
            };

            var entity = await context.GetAsync(playlist.Id, 
                new PartitionKeyBuilder().Add(playlist.SpredUserId.ToString()).Add(playlist.Bucket).Build(), CancellationToken.None);

            if (entity.Result == null)
            {
                await context.StoreAsync(playlist, CancellationToken.None);
                foreach (var stat in stats)
                    await statStore.StoreAsync(stat, CancellationToken.None);
            }
        }
    }
}
