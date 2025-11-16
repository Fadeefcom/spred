using MediatR;
using Spred.Bus.DTOs;
using TrackService.Components.Services;
using TrackService.Models.DTOs;
using TrackService.Models.Entities;

namespace TrackService.Models.Commands;

/// <summary>
/// Command to update an existing <see cref="TrackMetadata"/> record with new data from <see cref="TrackDto"/>.
/// Supports both full and partial updates of metadata and audio properties.
/// </summary>
public sealed class UpdateTrackMetadataItemCommand : INotification
{
    /// <summary>
    /// Initializes a new empty instance of the <see cref="UpdateTrackMetadataItemCommand"/> class.
    /// </summary>
    public UpdateTrackMetadataItemCommand()
    {
        PrimaryId = string.Empty;
        ContainerName = Environment.GetEnvironmentVariable("TRACK_CONTAINER_NAME") ?? "tracks";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTrackMetadataItemCommand"/> class from a <see cref="PrivateTrackDto"/>.
    /// </summary>
    public UpdateTrackMetadataItemCommand(PrivateTrackDto dto, Guid id, Guid spredUserId)
    {
        Id = id;
        SpredUserId = spredUserId;
        ContainerName = Environment.GetEnvironmentVariable("TRACK_CONTAINER_NAME") ?? "tracks";

        Title = dto.Title;
        Description = dto.Description;
        ImageUrl = dto.ImageUrl;
        Popularity = dto.Popularity;
        Published = dto.Published;
        UpdateAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTrackMetadataItemCommand"/> class from a <see cref="TrackDto"/>.
    /// </summary>
    /// <param name="dto">The updated track data transfer object.</param>
    /// <param name="id">The unique identifier of the existing track.</param>
    /// <param name="spredUserId">The unique identifier of the user associated with the track.</param>
    public UpdateTrackMetadataItemCommand(TrackDto dto, Guid id, Guid spredUserId)
    {
        Id = id;
        SpredUserId = spredUserId;
        ContainerName = Environment.GetEnvironmentVariable("TRACK_CONTAINER_NAME") ?? "tracks";

        Title = dto.Title;
        Description = dto.Description;
        ImageUrl = dto.ImageUrl;
        LanguageCode = dto.LanguageCode;
        ChartmetricsId = dto.ChartmetricsId;
        SoundChartsId = dto.SoundChartsId;
        Popularity = dto.Popularity;
        Published = dto.Published;
        SourceType = dto.SourceType;
        UpdateAt = DateTime.UtcNow;

        Audio = dto.Audio;
        Artists = dto.Artists;
        Album = dto.Album;
        
        UpdatedTrackLinks = dto.TrackUrl?.Select(x => new TrackLink
        {
            Platform = TrackPlatformLinkService.TryMap(x.Platform, out Platform platform)
                ? platform.ToString()
                : x.Platform,
            Value = x.Value?.ToString() ?? string.Empty
        }).ToList() ?? [];
    }

    /// <summary>
    /// Gets or sets the unique identifier of the track to update.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the user ID that owns the track.
    /// </summary>
    public Guid SpredUserId { get; init; }

    /// <summary>
    /// Gets or sets the external primary identifier of the track.
    /// </summary>
    public string? PrimaryId { get; init; }

    /// <summary>
    /// Gets or sets the updated track title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the updated track description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the updated image URL of the track.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the ISO language code of the track.
    /// </summary>
    public string? LanguageCode { get; set; }

    /// <summary>
    /// Gets or sets the Chartmetrics internal track ID.
    /// </summary>
    public string? ChartmetricsId { get; set; }

    /// <summary>
    /// Gets or sets the SoundCharts internal track ID.
    /// </summary>
    public string? SoundChartsId { get; set; }

    /// <summary>
    /// Gets or sets the audio features for the track.
    /// </summary>
    public AudioFeaturesDto? Audio { get; set; }

    /// <summary>
    /// Gets or sets the list of associated artists.
    /// </summary>
    public List<ArtistDto>? Artists { get; set; }

    /// <summary>
    /// Gets or sets the album information.
    /// </summary>
    public AlbumDto? Album { get; set; }

    /// <summary>
    /// Gets or sets the popularity score of the track.
    /// </summary>
    public uint? Popularity { get; set; }

    /// <summary>
    /// Gets or sets the publication date of the track.
    /// </summary>
    public DateTime Published { get; set; }

    /// <summary>
    /// Gets the last update timestamp.
    /// </summary>
    public DateTime UpdateAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the source type of the track.
    /// </summary>
    public SourceType? SourceType { get; protected set; }

    /// <summary>
    /// Gets or sets the current upload status (two-phase commit).
    /// </summary>
    public UploadStatus? Status { get; set; }

    /// <summary>
    /// Gets the container name where the file is stored.
    /// </summary>
    public string ContainerName { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets a normalized list of updated track links built from <see cref="TrackUrls"/>.
    /// </summary>
    public List<TrackLink> UpdatedTrackLinks { get; private set; } = [];

    /// <summary>
    /// Updates the technical metadata of the track using audio analysis results.
    /// </summary>
    /// <param name="analyzeTrackDto">The analysis results DTO.</param>
    public void UpdateByTrackAnalayze(AnalyzeTrackDto analyzeTrackDto)
    {
        if (analyzeTrackDto == null)
            return;

        Audio ??= new AudioFeaturesDto();

        Audio.Duration = analyzeTrackDto.Duration;
        Audio.Bitrate = analyzeTrackDto.Bitrate;
        Audio.SampleRate = analyzeTrackDto.SampleRate;
        Audio.Channels = analyzeTrackDto.Channels;
        Audio.Bpm = analyzeTrackDto.Bpm;
        Audio.Codec = analyzeTrackDto.Codec;
    }

    /// <summary>
    /// Updates the track's image URL after external upload or processing.
    /// </summary>
    /// <param name="imageUrl">The new image URL.</param>
    public void UpdateImageUrl(string imageUrl)
    {
        if (!string.IsNullOrWhiteSpace(imageUrl))
            ImageUrl = imageUrl;
    }
}
