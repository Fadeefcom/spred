using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Extensions.Utilities;
using PlaylistService.Models.Commands;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;

namespace PlaylistService.Models.Entities;

/// <summary>
/// Represents metadata information for catalog playlists.
/// Provides persistence and update logic with full synchronization
/// to the new MetadataBaseDto structure.
/// </summary>
public class CatalogMetadata : BaseEntity<CatalogMetadata, CreateMetadataCommand, UpdateMetadataCommand, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogMetadata"/> class
    /// with default values and empty collections.
    /// </summary>
    public CatalogMetadata()
    {
        PrimaryId = new PrimaryId();
        Name = string.Empty;
        Description = string.Empty;
        Tracks = [];
        ListenUrls = new Dictionary<string, string>();
        SubmitUrls = new Dictionary<string, string>();
        SubmitEmail = string.Empty;
        ImageUrl = string.Empty;
        ChartmetricsId = string.Empty;
        SoundChartsId = string.Empty;
        Href = string.Empty;
        Tags = [];
        OwnerPlatformId = string.Empty;
        OwnerPlatformName = string.Empty;
        UserPlatformId = string.Empty;
        Moods = [];
        Activities = [];
        CatalogType = string.Empty;
        Id = Guid.NewGuid();
        Bucket = "";
        Type = string.Empty;
    }

    /// <summary>
    /// The metadata entity type.
    /// </summary>
    [StringLength(10)]
    public string Type { get; protected set; }

    /// <summary>
    /// External platform identifier (e.g., Spotify ID).
    /// </summary>
    [StringLength(1000)]
    public PrimaryId PrimaryId { get; protected set; }

    /// <summary>
    /// The unique Spred user identifier.
    /// </summary>
    [PartitionKey]
    public Guid SpredUserId { get; private set; }

    /// <summary>
    /// The partition bucket identifier.
    /// </summary>
    [StringLength(4)]
    [PartitionKey(1)]
    public string Bucket { get; private set; }

    /// <summary>
    /// The Chartmetric internal identifier.
    /// </summary>
    public string ChartmetricsId { get; private set; }

    /// <summary>
    /// The SoundCharts internal identifier.
    /// </summary>
    public string SoundChartsId { get; private set; }

    /// <summary>
    /// Playlist or catalog name.
    /// </summary>
    [StringLength(100)]
    public string Name { get; private set; }

    /// <summary>
    /// Playlist or catalog description.
    /// </summary>
    [StringLength(1000)]
    public string Description { get; private set; }

    /// <summary>
    /// Total number of tracks.
    /// </summary>
    public uint TracksTotal { get; private set; }

    /// <summary>
    /// Total number of followers.
    /// </summary>
    public uint Followers { get; private set; }

    /// <summary>
    /// Indicates whether the playlist is public.
    /// </summary>
    public bool IsPublic { get; protected set; }

    /// <summary>
    /// Indicates whether the playlist is collaborative.
    /// </summary>
    public bool Collaborative { get; private set; }

    /// <summary>
    /// Playlist tags and keywords.
    /// </summary>
    public List<string> Tags { get; private set; }

    /// <summary>
    /// Playlist track identifiers.
    /// </summary>
    [JsonInclude]
    public HashSet<Guid> Tracks { get; private set; }

    /// <summary>
    /// Owner display name on platform.
    /// </summary>
    public string OwnerPlatformName { get; private set; }

    /// <summary>
    /// Platform-specific owner ID.
    /// </summary>
    public string OwnerPlatformId { get; private set; }

    /// <summary>
    /// Linked user ID in platform.
    /// </summary>
    public string UserPlatformId { get; private set; }

    /// <summary>
    /// Catalog classification type.
    /// </summary>
    public string CatalogType { get; private set; }

    /// <summary>
    /// Ratio of active tracks or engagement over lifetime.
    /// </summary>
    public double ActiveRatio { get; private set; }

    /// <summary>
    /// Suspicious activity score.
    /// </summary>
    public double SuspicionScore { get; private set; }

    /// <summary>
    /// Playlist moods ranked by priority.
    /// </summary>
    public Dictionary<int, string> Moods { get; private set; }

    /// <summary>
    /// Playlist activities ranked by priority.
    /// </summary>
    public Dictionary<int, string> Activities { get; private set; }

    /// <summary>
    /// Playlist image URL.
    /// </summary>
    [StringLength(1000)]
    public string ImageUrl { get; private set; }

    /// <summary>
    /// Main entity HREF link.
    /// </summary>
    [StringLength(1000)]
    public string Href { get; private set; }

    /// <summary>
    /// Email address for track submissions.
    /// </summary>
    [StringLength(1000)]
    public string SubmitEmail { get; private set; }

    /// <summary>
    /// Listen URLs mapped by platform.
    /// </summary>
    [JsonInclude]
    public IDictionary<string, string> ListenUrls { get; private set; }

    /// <summary>
    /// Submit URLs mapped by platform.
    /// </summary>
    [JsonInclude]
    public IDictionary<string, string> SubmitUrls { get; private set; }

    /// <summary>
    /// Indicates whether the metadata entry is deleted.
    /// </summary>
    public bool IsDeleted { get; protected set; }

    /// <summary>
    /// Creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last update timestamp (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Flag indicating whether statistics should be refreshed.
    /// </summary>
    public bool NeedUpdateStatInfo { get; set; }

    /// <inheritdoc/>
    public override void Create(CreateMetadataCommand command)
    {
        SpredUserId = command.SpredUserId;
        Bucket = SpredUserId == Guid.Empty ? GuidShortener.GenerateBucketFromGuid(Id) : command.Bucket;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        NeedUpdateStatInfo = true;

        PrimaryId = command.PrimaryId;
        Name = command.Name ?? string.Empty;
        Description = command.Description ?? string.Empty;
        Tracks = command.Tracks.Count != 0 ? [..command.Tracks] : [];
        TracksTotal = (uint)Tracks.Count;
        Followers = command.Followers ?? 0;
        IsPublic = command.IsPublic;
        Collaborative = command.Collaborative;
        ListenUrls = command.ListenUrls ?? [];
        SubmitUrls = command.SubmitUrls ?? [];
        SubmitEmail = command.SubmitEmail ?? string.Empty;
        ImageUrl = command.ImageUrl ?? string.Empty;
        Href = command.Href ?? string.Empty;
        ChartmetricsId = command.ChartmetricsId ?? string.Empty;
        SoundChartsId = command.SoundChartsId ?? string.Empty;
        Tags = command.Tags ?? [];
        OwnerPlatformName = command.OwnerPlatformName ?? string.Empty;
        OwnerPlatformId = command.OwnerPlatformId ?? string.Empty;
        UserPlatformId = command.UserPlatformId ?? string.Empty;
        CatalogType = command.CatalogType ?? string.Empty;
        ActiveRatio = command.ActiveRatio ?? 0;
        SuspicionScore = command.SuspicionScore ?? 0;
        Moods = command.Moods ?? [];
        Activities = command.Activities ?? [];
    }

    /// <inheritdoc/>
    public override void Update(UpdateMetadataCommand metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.Name)) Name = metadata.Name;
        if (!string.IsNullOrWhiteSpace(metadata.Description)) Description = metadata.Description;
        if (!string.IsNullOrWhiteSpace(metadata.Href)) Href = metadata.Href;

        if (metadata.TracksTotal.HasValue) TracksTotal = metadata.TracksTotal.Value;
        if (metadata.Followers.HasValue) Followers = metadata.Followers.Value;
        if (metadata.IsPublic.HasValue) IsPublic = metadata.IsPublic.Value;
        if (metadata.Collaborative.HasValue) Collaborative = metadata.Collaborative.Value;

        if (metadata.Tracks.Count > 0) Tracks = [..metadata.Tracks];
        if (!string.IsNullOrWhiteSpace(metadata.ImageUrl)) ImageUrl = metadata.ImageUrl;
        if (!string.IsNullOrWhiteSpace(metadata.SubmitEmail)) SubmitEmail = metadata.SubmitEmail;
        if (!string.IsNullOrWhiteSpace(metadata.ChartmetricsId)) ChartmetricsId = metadata.ChartmetricsId;
        if (!string.IsNullOrWhiteSpace(metadata.SoundChartsId)) SoundChartsId = metadata.SoundChartsId;
        if (!string.IsNullOrWhiteSpace(metadata.OwnerPlatformId)) OwnerPlatformId = metadata.OwnerPlatformId;
        if (!string.IsNullOrWhiteSpace(metadata.OwnerPlatformName)) OwnerPlatformName = metadata.OwnerPlatformName;
        if (!string.IsNullOrWhiteSpace(metadata.UserPlatformId)) UserPlatformId = metadata.UserPlatformId;

        if (!string.IsNullOrEmpty(metadata.CatalogType))
            CatalogType = metadata.CatalogType;

        if (metadata.ActiveRatio.HasValue) ActiveRatio = metadata.ActiveRatio.Value;
        if (metadata.SuspicionScore.HasValue) SuspicionScore = metadata.SuspicionScore.Value;

        if (metadata.Moods is { Count: > 0 })
            foreach (var kv in metadata.Moods)
                Moods.TryAdd(kv.Key, kv.Value);

        if (metadata.Activities is { Count: > 0 })
            foreach (var kv in metadata.Activities)
                Activities.TryAdd(kv.Key, kv.Value);

        if (metadata.Tags is { Count: > 0 })
            foreach (var tag in metadata.Tags)
                if (!Tags.Contains(tag))
                    Tags.Add(tag);

        foreach (var kv in metadata.ListenUrls ?? [])
            if(!ListenUrls.TryAdd(kv.Key, kv.Value))
                ListenUrls[kv.Key] = kv.Value;

        foreach (var kv in metadata.SubmitUrls ?? [])
            if(!SubmitUrls.TryAdd(kv.Key, kv.Value))
                SubmitUrls[kv.Key] = kv.Value;

        if (NeedUpdateStatInfo)
            NeedUpdateStatInfo = !metadata.StatsUpdated;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public override void Delete()
    {
        IsDeleted = true;
    }
}
