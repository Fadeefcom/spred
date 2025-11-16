using System.ComponentModel.DataAnnotations;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;
using Spred.Bus.DTOs;
using TrackService.Models.Commands;

namespace TrackService.Models.Entities;

/// <summary>
/// Defines a class that stores track-related metadata including attributes such as title, description, genre,
/// associated artists, platform links, audio features, and various identifiers.
/// </summary>
public class TrackMetadata : BaseEntity<TrackMetadata, CreateTrackMetadataItemCommand, UpdateTrackMetadataItemCommand,
    Guid>
{
    /// <summary>
    /// .ctor
    /// </summary>
    public TrackMetadata()
    {
        Id = Guid.NewGuid();
        PrimaryId = string.Empty;
        Title = string.Empty;
        Description = string.Empty;
        ImageUrl = string.Empty;
        LanguageCode = string.Empty;
        ChartmetricsId = string.Empty;
        SoundChartsId = string.Empty;
        ContainerName = string.Empty;
        Bucket = "00";
        Artists = [];
        TrackLinks = [];
        Audio = new AudioFeatures();
    }

    [PartitionKey]
    public Guid SpredUserId { get; protected set; }

    /// <summary>
    /// Represents the bucket associated with the track metadata, used for partitioning data storage.
    /// </summary>
    /// <remarks>
    /// This property is leveraged to categorize tracks into different buckets for system-level organization and distribution.
    /// </remarks>
    [PartitionKey(1)]
    public string Bucket { get; protected set; }

    /// <summary>
    /// Gets or sets the primary identifier for the track metadata.
    /// </summary>
    /// <remarks>
    /// This property uniquely identifies a track metadata entry within the system.
    /// It is limited to a maximum length of 100 characters.
    /// </remarks>
    [StringLength(100)]
    public string PrimaryId { get; protected set; }

    [StringLength(200)]
    public string Title { get; protected set; }

    [StringLength(1000)]
    public string Description { get; protected set; }

    [StringLength(1000)]
    public string ImageUrl { get; protected set; }

    [StringLength(100)]
    public string LanguageCode { get; protected set; }

    public string ChartmetricsId { get; protected set; }

    /// <summary>
    /// Gets the unique identifier associated with the track in the SoundCharts system.
    /// </summary>
    /// <remarks>
    /// This property is used to store an external reference ID for integrating with the SoundCharts platform.
    /// It is typically populated during creation or update operations when data from SoundCharts is available.
    /// </remarks>
    public string SoundChartsId { get; protected set; }

    public AudioFeatures Audio { get; protected set; } = new();

    public IList<Artist> Artists { get; protected set; }
    public Album? Album { get; protected set; }
    public IList<TrackLink> TrackLinks { get; protected set; }

    public uint Popularity { get; protected set; }
    public SourceType SourceType { get; protected set; }

    public DateTime Published { get; protected set; }
    public DateTime AddedAt { get; protected set; }
    public DateTime UpdateAt { get; protected set; }

    public string ContainerName { get; protected set; }

    /// Represents the deletion status of a track metadata entity.
    /// This property is used to indicate whether the specific track metadata entity
    /// has been marked as deleted. A value of `true` signifies that the entity is
    /// considered deleted, while `false` means it is active.
    /// The value of this property is typically influenced by the execution of the
    /// `Delete` method, which is intended to set `IsDeleted` to `true`.
    /// Note: The property is set to 'protected' for modifications, ensuring it is
    /// only altered within the entity itself or derived classes.
    public bool IsDeleted { get; protected set; }
    public UploadStatus Status { get; protected set; }

    /// <inheritdoc/>
    public override void Create(CreateTrackMetadataItemCommand command)
    {
        Id = command.Id;
        SpredUserId = command.SpredUserId;
        Bucket = command.Bucket;

        PrimaryId = command.PrimaryId;
        Title = command.Title;
        Description = command.Description;
        ImageUrl = command.ImageUrl;
        LanguageCode = command.LanguageCode;
        ChartmetricsId = command.ChartmetricsId;
        SoundChartsId = command.SoundChartsId;
        Popularity = command.Popularity;
        SourceType = command.SourceType;
        Published = command.Published;
        AddedAt = command.AddedAt;
        UpdateAt = command.UpdateAt;
        ContainerName = command.ContainerName;
        Status = UploadStatus.Pending;

        Audio = new AudioFeatures
        {
            Bpm = command.Audio.Bpm,
            Bitrate = command.Audio.Bitrate,
            Channels = command.Audio.Channels,
            Duration = command.Audio.Duration,
            Codec = command.Audio.Codec,
            Genre = command.Audio.Genre,
            SampleRate = command.Audio.SampleRate,
            Energy = command.Audio.Energy,
            Valence = command.Audio.Valence
        };

        Artists = command.Artists;
        Album = command.Album;
        TrackLinks = command.TrackLinks;
    }

    /// Updates the track metadata using the provided command.
    /// The method updates values such as title, description, image URL, language code, and others if they are provided in the command.
    /// It also handles additional updates for related entities like audio properties, artists, albums, and track URLs where applicable.
    /// <param name="command">An instance of <see cref="UpdateTrackMetadataItemCommand"/> containing updated values for the track metadata.</param>
    public override void Update(UpdateTrackMetadataItemCommand command)
    {
        if (!string.IsNullOrWhiteSpace(command.Title)) Title = command.Title;
        if (!string.IsNullOrWhiteSpace(command.Description)) Description = command.Description;
        if (!string.IsNullOrWhiteSpace(command.ImageUrl)) ImageUrl = command.ImageUrl;
        if (!string.IsNullOrWhiteSpace(command.LanguageCode)) LanguageCode = command.LanguageCode;
        if (!string.IsNullOrWhiteSpace(command.ChartmetricsId)) ChartmetricsId = command.ChartmetricsId;
        if (!string.IsNullOrWhiteSpace(command.SoundChartsId)) SoundChartsId = command.SoundChartsId;
        if (command.Popularity.HasValue) Popularity = command.Popularity.Value;
        if (command.SourceType.HasValue) SourceType = command.SourceType.Value;

        // Audio features
        if (command.Audio is not null)
        {
            Audio.UpdateFrom(command.Audio);
        }

        // Artists update
        if (command.Artists is { Count: > 0 })
            Artists = command.Artists.Select(a => new Artist { Name = a.Name, PrimaryId = a.PrimaryId, ImageUrl = a.ImageUrl }).ToList();

        // Album update
        if (command.Album is not null)
        {
            Album = new Album
            {
                PrimaryId = command.Album.PrimaryId,
                AlbumName = command.Album.AlbumName,
                AlbumLabel = command.Album.AlbumLabel,
                AlbumReleaseDate = command.Album.AlbumReleaseDate,
                ImageUrl = command.Album.ImageUrl
            };
        }

        foreach (var link in command.UpdatedTrackLinks)
        {
            var existing = TrackLinks.FirstOrDefault(x => x.Platform == link.Platform);
            if (existing == null)
                TrackLinks.Add(link);
            else
                existing.Value = link.Value;
        }

        Status = command.Status ?? Status;
        UpdateAt = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public override void Delete() => IsDeleted = true;
    
    /// <summary>
    /// Change status to created
    /// </summary>
    public void StatusCreated()
    {
        Status = UploadStatus.Created;
    }

    /// <summary>
    /// Reset id
    /// </summary>
    public void ResetId()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Encapsulates detailed audio properties, including technical and perceptual characteristics,
/// such as duration, bitrate, codec, tempo, and audio energy levels.
/// </summary>
public class AudioFeatures
{
    public TimeSpan Duration { get; set; }
    public uint Bitrate { get; set; }
    public uint SampleRate { get; set; }
    public uint Channels { get; set; }
    public string Codec { get; set; } = string.Empty;
    public ushort Bpm { get; set; }
    public string Genre { get; set; } = string.Empty;
    public double Energy { get; set; }

    /// <summary>
    /// Represents the valence of the audio track, indicating the musical positivity of the track.
    /// The value is typically a floating point number between 0.0 and 1.0, where values closer to
    /// 1.0 represent a higher degree of musical positivity or happiness, while values closer to
    /// 0.0 indicate a more negative or melancholic quality.
    /// </summary>
    public double Valence { get; set; }

    public void UpdateFrom(AudioFeaturesDto dto)
    {
        if (dto.Duration != TimeSpan.Zero) Duration = dto.Duration;
        if (dto.Bitrate != 0) Bitrate = dto.Bitrate;
        if (dto.SampleRate != 0) SampleRate = dto.SampleRate;
        if (dto.Channels != 0) Channels = dto.Channels;
        if (!string.IsNullOrWhiteSpace(dto.Codec)) Codec = dto.Codec;
        if (dto.Bpm != 0) Bpm = dto.Bpm;
        if (!string.IsNullOrWhiteSpace(dto.Genre)) Genre = dto.Genre;
        if (dto.Energy > 0) Energy = dto.Energy;
        if (dto.Valence > 0) Valence = dto.Valence;
    }
}