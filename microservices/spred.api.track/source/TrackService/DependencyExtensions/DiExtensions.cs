using Extensions.Models;
using Extensions.Utilities;
using Microsoft.Extensions.Options;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Repositories;
using Spred.Bus.DTOs;
using StackExchange.Redis;
using TrackService.Abstractions;
using TrackService.Components.Services;
using TrackService.Configuration;
using TrackService.Models;
using TrackService.Models.Commands;
using TrackService.Models.Entities;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace TrackService.DependencyExtensions;

/// <summary>
/// Provides extension methods for configuring dependency injection in the TrackService application.
/// </summary>
public static class DiExtensions
{
    /// <summary>
    /// Configures the BlobOptions using the provided configuration.
    /// </summary>
    /// <param name="serviceCollection">The service collection to add the options to.</param>
    /// <param name="configuration">The configuration to bind the options from.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection ConfigureBlobOptions(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.AddOptions<BlobOptions>().Bind(configuration.GetSection(BlobOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrWhiteSpace(options.ContainerName) ||
                                 !string.IsNullOrWhiteSpace(options.BlobConnectString), "Blob options is empty.")
            .ValidateOnStart();
        return serviceCollection;
    }

    /// <summary>
    /// Adds application stores and repositories to the service collection.
    /// </summary>
    /// <param name="serviceCollection">The service collection to add the stores to.</param>
    /// <param name="configuration">The configuration to use for the stores.</param>
    /// <param name="isProduction">Indicates whether the application is running in production.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddApplicationStores(this IServiceCollection serviceCollection, IConfiguration configuration, bool isProduction)
    {
        serviceCollection.AddCosmosClient();
        serviceCollection.AddContainer<TrackMetadata>([], version: 2);
        serviceCollection.AddContainer<TrackPlatformId>([], uniqueKeys:[ [ "/Platform", "/PlatformTrackId" ] ]);

        // Add repository in pipeline
        if (isProduction)
            serviceCollection.AddScoped<IBlobContainerProvider, BlobContainer>();
        else
            serviceCollection.AddScoped<IBlobContainerProvider, BlobContainerLocal>();

        serviceCollection.AddScoped<IUploadTrackService, UploadTrackService>();
        serviceCollection.AddScoped<ITrackManager, TrackManager>();
        serviceCollection.AddScoped<IPersistenceStore<TrackMetadata, Guid>, PersistenceStore<TrackMetadata, Guid>>();
        serviceCollection.AddScoped<IPersistenceStore<TrackPlatformId, Guid>, PersistenceStore<TrackPlatformId, Guid>>();
        serviceCollection.AddSingleton<IConnectionMultiplexer>((serviceProvider) =>
        {
            var redisOptions = serviceProvider.GetRequiredService<IOptions<RedisOptions>>();
            var options = ConfigurationOptions.Parse(redisOptions.Value.ConnectionString);
            return ConnectionMultiplexer.Connect(options);
        });

        return serviceCollection;
    }

    /// <summary>
    /// Create test tracks in db
    /// </summary>
    /// <param name="scope"></param>
    public static async Task InitTestTracks(this IServiceScope scope)
    {
        var manager = scope.ServiceProvider.GetRequiredService<ITrackManager>();
        List<Guid> ids = [Guid.Parse("95937e91-4baf-4c82-822e-f4ded431412e"),
                    Guid.Parse("1eca47bb-92e9-4b71-a20d-0eeac231c245"),
                    Guid.Parse("8a7e65d2-46f6-4df1-a4aa-a53b365034a5")];

        foreach (var id in ids)
        {

            var entity = new TrackMetadata();
            var command = new CreateTrackMetadataItemCommand(new TrackDto()
            {
                Title = "Test Track",
                PrimaryId = "empty",
                Description = "Test Track Description",
                Artists = [new ArtistDto()
                {
                    Name = "Test Artist",
                    PrimaryId = "empty"
                }],
                Album = new AlbumDto()
                {
                    AlbumName = "Test Artist",
                    PrimaryId = "empty"
                }
            }, Guid.Empty, null)
            {
                Id = id
            };

            entity.Create(command);

            var bucket = GuidShortener.GenerateBucketFromGuid(id);
            var track = await manager.GetByIdAsync(id, Guid.Empty, CancellationToken.None, bucket);
            if (track == null)
                await manager.AddAsync(entity, entity.SpredUserId);
        }
    }

    /// <summary>
    /// Download ffmpeg
    /// </summary>
    /// <returns></returns>
    public static async Task DownloadFfmpeg()
    {
        string ffmpeg = Path.Combine(Environment.CurrentDirectory, "Ffmpeg");
        if (!Directory.Exists(ffmpeg))
        {
            Console.WriteLine("Download Ffmpeg.");
            Directory.CreateDirectory("Ffmpeg");
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpeg);
            Console.WriteLine("Download completed Ffmpeg.");
        }

        FFmpeg.SetExecutablesPath(ffmpeg, formatprovider: default);
        try
        {
            IConversionResult value = await FFmpeg.Conversions.New().Start("-version");
            Console.WriteLine($"Ffmpeg is installed correctly. v - {value}");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine("Ffmpeg validation failed: " + ex.Message);
        }

        string path1 = Path.Combine(Environment.CurrentDirectory, Names.VideoFiles);
        if (!Directory.Exists(path1))
        {
            Directory.CreateDirectory(path1);
        }

        string path2 = Path.Combine(Environment.CurrentDirectory, Names.AudioFiles);
        if (!Directory.Exists(path2))
        {
            Directory.CreateDirectory(path2);
        }
    }
}
