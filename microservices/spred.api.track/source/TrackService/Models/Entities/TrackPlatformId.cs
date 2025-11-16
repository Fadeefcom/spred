using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;

namespace TrackService.Models.Entities;

/// <summary>
/// Represents the entity that associates track metadata with a specific platform and its corresponding identifier.
/// </summary>
public sealed class TrackPlatformId : IBaseEntity<Guid>
{
    /// Represents a unique platform identifier for a track in the music service platform.
    /// This class implements the IBaseEntity interface to provide an Id, ETag, and Timestamp properties.
    public TrackPlatformId()
    {
        Id = Guid.NewGuid();
    }

    /// Represents the unique identifier for the metadata associated with a track.
    public required Guid TrackMetadataId { get; init; }
    
    /// <summary>
    /// Associated user ID.
    /// </summary>
    public required Guid SpredUserId { get; init; }

    /// Represents the platform associated with the track. This enumerable property defines the streaming
    /// platforms available for tracks, including Spotify, YouTube, SoundCloud, Apple Music, and Deezer.
    [PartitionKey]
    public required Platform  Platform { get; init; }

    /// Represents the unique identifier for a track on a specific platform.
    [StringLength(100)]
    public required string PlatformTrackId { get; init; }

    /// <summary>
    /// Gets the unique identifier for the instance of <see cref="TrackPlatformId"/>.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Represents the entity tag (ETag) associated with the resource.
    /// Used for concurrency control and ensuring consistency during updates.
    /// </summary>
    public string? ETag { get; private set; }

    /// <summary>
    /// Represents the timestamp associated with the entity.
    /// </summary>
    public long Timestamp { get; private set; }
}

/// <summary>
/// Represents the Apple Music platform within the <see cref="Platform"/> enumeration.
/// </summary>
/// <remarks>
/// This enum member is used to indicate that the track platform is Apple Music.
/// Typically utilized within the <see cref="TrackPlatformId"/> class and other related entities
/// to specify the platform associated with a track.
/// </remarks>
[JsonConverter(typeof(StringEnumConverter))]
public enum Platform
{
    /// <summary>
    /// Represents Spotify as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    Spotify,

    /// <summary>
    /// Represents YouTube as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    YouTube,

    /// <summary>
    /// Represents YouTube Music as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    YouTubeMusic,

    /// <summary>
    /// Represents the SoundCloud platform in the <see cref="Platform"/> enumeration.
    /// </summary>
    SoundCloud,

    /// <summary>
    /// Represents the Apple Music platform within the <see cref="Platform"/> enumeration.
    /// </summary>
    AppleMusic,

    /// <summary>
    /// Represents the Deezer platform in the enumeration of supported platforms for tracking music metadata.
    /// </summary>
    Deezer,

    /// <summary>
    /// Represents the International Standard Recording Code (ISRC) as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    ISRC,

    /// <summary>
    /// Represents the Shazam platform as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    Shazam,

    /// <summary>
    /// Represents TikTok as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    TikTok,

    /// <summary>
    /// Represents KKBox as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    KKBox,

    /// <summary>
    /// Represents Amazon Music as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    AmazonMusic,

    /// <summary>
    /// Represents Pandora as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    Pandora,

    /// <summary>
    /// Represents Anghami as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    Anghami,

    /// <summary>
    /// Represents Boomplay as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    Boomplay,

    /// <summary>
    /// Represents Tencent / QQ Music as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    Tencent,

    /// <summary>
    /// Represents NetEase Cloud Music as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    NetEase,

    /// <summary>
    /// Represents VK Music as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    VKMusic,

    /// <summary>
    /// Represents Napster as a value in the <see cref="Platform"/> enumeration.
    /// </summary>
    Napster,
}