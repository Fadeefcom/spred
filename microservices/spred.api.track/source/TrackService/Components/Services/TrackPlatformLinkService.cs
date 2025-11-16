using Extensions.Extensions;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Interfaces;
using Spred.Bus.Contracts;
using TrackService.Models.Entities;

namespace TrackService.Components.Services;

/// <summary>
/// Provides functionality for linking track metadata with platform-specific identifiers.
/// </summary>
public class TrackPlatformLinkService
{
    private readonly IPersistenceStore<TrackPlatformId, Guid> _store;
    private readonly ILogger<TrackPlatformLinkService> _logger;
    
    private static readonly Dictionary<string, Platform> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["spotify"] = Platform.Spotify,

        ["youtube"] = Platform.YouTube,
        ["youtube music"] = Platform.YouTubeMusic,
        ["youtubemusic"] = Platform.YouTubeMusic,
        ["youtube_music"] = Platform.YouTubeMusic,
        ["youtube-music"] = Platform.YouTubeMusic,

        ["soundcloud"] = Platform.SoundCloud,

        ["applemusic"] = Platform.AppleMusic,
        ["apple music"] = Platform.AppleMusic,
        ["apple_music"] = Platform.AppleMusic,
        ["apple-music"] = Platform.AppleMusic,

        ["deezer"] = Platform.Deezer,

        ["isrc"] = Platform.ISRC,
        ["shazam"] = Platform.Shazam,

        ["tiktok"] = Platform.TikTok,

        ["kkbox"] = Platform.KKBox,

        ["amazonmusic"] = Platform.AmazonMusic,
        ["amazon music"] = Platform.AmazonMusic,
        ["amazon_music"] = Platform.AmazonMusic,
        ["amazon-music"] = Platform.AmazonMusic,

        ["pandora"] = Platform.Pandora,

        ["anghami"] = Platform.Anghami,

        ["boomplay"] = Platform.Boomplay,

        ["tencent"] = Platform.Tencent,
        ["qqmusic"] = Platform.Tencent,
        ["qq music"] = Platform.Tencent,
        ["qq_music"] = Platform.Tencent,
        ["qq-music"] = Platform.Tencent,

        ["netease"] = Platform.NetEase,
        ["netease cloud music"] = Platform.NetEase,
        ["neteasemusic"] = Platform.NetEase,
        ["netease_music"] = Platform.NetEase,
        ["netease-music"] = Platform.NetEase,

        ["vkmusic"] = Platform.VKMusic,
        ["vk music"] = Platform.VKMusic,
        ["vk_music"] = Platform.VKMusic,
        ["vk-music"] = Platform.VKMusic,
        
        ["napster"] = Platform.Napster,
        ["napster music"] = Platform.Napster,
        ["napster_music"] = Platform.Napster,
        ["napster-music"] = Platform.Napster,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackPlatformLinkService"/> class.
    /// </summary>
    /// <param name="store">
    /// The persistence store implementation used to access and manage <see cref="TrackPlatformId"/> entities.
    /// </param>
    /// <param name="loggerFactory">Logger factory.</param>
    public TrackPlatformLinkService(IPersistenceStore<TrackPlatformId, Guid> store, ILoggerFactory loggerFactory)
    {
        _store = store;
        _logger = loggerFactory.CreateLogger<TrackPlatformLinkService>();
    }

    /// <summary>
    /// Attempts to resolve the <see cref="TrackPlatformId"/> for the given list of platform identifier pairs.
    /// </summary>
    /// <param name="platformIds">
    /// A collection of <see cref="PlatformIdPair"/> items containing the platform name and primary identifier.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    /// The <see cref="Guid"/> of the associated track metadata if a match is found; otherwise, <c>null</c>.
    /// </returns>
    public async Task<Guid?> GetLinkAsync(List<PlatformIdPair> platformIds, CancellationToken cancellationToken)
    {
        foreach (var pair in platformIds)
        {
            if (!TryMap(pair.Platform, out var platform))
            {
                _logger.LogSpredWarning(
                    "PlatformMapping",
                    $"Unknown platform '{pair.Platform}' encountered while mapping TrackPlatformId."
                );
                continue;
            }

            var result = await _store.GetAsync(
                predicate: x => x.PlatformTrackId == pair.PrimaryId,
                sortSelector: x => x.Timestamp,
                partitionKey: new PartitionKey(platform.ToString()),
                offset: 0,
                limit: 1,
                descending: false,
                cancellationToken: cancellationToken);

            if (result is { IsSuccess: true, Result: not null } && result.Result.Any())
            {
                return result.Result.First().TrackMetadataId;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds new platform links for the specified track metadata identifiers.
    /// </summary>
    public async Task AddLinksAsync(List<PlatformIdPair> platformIds, Guid spredUserId, Guid trackId, CancellationToken cancellationToken)
    {
        foreach (var pair in platformIds)
        {
            if (!TryMap(pair.Platform, out var platform))
            {
                _logger.LogSpredWarning(
                    "PlatformMapping",
                    $"Unknown platform '{pair.Platform}' encountered while mapping TrackPlatformId."
                );
                continue;
            }

            var entity = new TrackPlatformId
            {
                TrackMetadataId = trackId,
                SpredUserId = spredUserId,
                Platform = platform,
                PlatformTrackId = pair.PrimaryId
            };

            await _store.StoreAsync(entity, cancellationToken);
        }
    }
    
    /// <summary>
    /// Try Map platform name to platform enum.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="platform"></param>
    /// <returns></returns>
    public static bool TryMap(string value, out Platform platform)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            platform = default;
            return false;
        }

        return _map.TryGetValue(value.ToLowerInvariant().Trim(), out platform);
    }
}