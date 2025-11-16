using Extensions.Utilities;
using MediatR;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;
using TrackService.Components.Services;
using TrackService.Models.DTOs;
using TrackService.Models.Entities;

namespace TrackService.Models.Commands;

/// <summary>
/// Command to create a new track metadata record based on <see cref="TrackDto"/> or <see cref="TrackDtoWithPlatformIds"/>.
/// This command encapsulates all necessary fields for initializing <see cref="TrackMetadata"/> entity.
/// </summary>
public sealed class CreateTrackMetadataItemCommand : IRequest<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTrackMetadataItemCommand"/> class using <see cref="TrackDto"/>.
    /// </summary>
    /// <param name="dto">The track data transfer object.</param>
    /// <param name="spredUserId">The unique identifier of the user who owns the track.</param>
    /// <param name="file">The uploaded audio file (optional).</param>
    public CreateTrackMetadataItemCommand(TrackDto dto, Guid spredUserId, IFormFile? file)
    {
        Initialize([], dto, spredUserId, file);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTrackMetadataItemCommand"/> class using <see cref="TrackDtoWithPlatformIds"/>.
    /// </summary>
    /// <param name="dto">The track data transfer object with platform IDs.</param>
    /// <param name="spredUserId">The unique identifier of the user who owns the track.</param>
    /// <param name="file">The uploaded audio file (optional).</param>
    public CreateTrackMetadataItemCommand(TrackDtoWithPlatformIds dto, Guid spredUserId, IFormFile? file)
    {
        Initialize(dto.PrimaryIds, dto, spredUserId, file);
    }

    /// <summary>
    /// Initializes core fields shared by both constructors.
    /// </summary>
    private void Initialize(List<PlatformIdPair> platformIds, TrackDto dto, Guid spredUserId, IFormFile? file)
    {
        Id = Guid.NewGuid();
        SpredUserId = spredUserId;
        FormFile = file;

        PrimaryId = dto.PrimaryId;
        Title = dto.Title;
        Description = dto.Description;
        ImageUrl = dto.ImageUrl;
        LanguageCode = dto.LanguageCode;
        ChartmetricsId = dto.ChartmetricsId;
        SoundChartsId = dto.SoundChartsId;
        Popularity = dto.Popularity;
        Published = dto.Published;
        AddedAt = dto.AddedAt;
        UpdateAt = dto.UpdateAt;
        SourceType = dto.SourceType;

        Audio = dto.Audio;
        Artists = dto.Artists.Select(a => new Artist
        {
            PrimaryId = a.PrimaryId,
            Name = a.Name,
            ImageUrl = a.ImageUrl
        }).ToList();

        Album = dto.Album is null ? null : new Album
        {
            AlbumName = dto.Album.AlbumName,
            AlbumLabel = dto.Album.AlbumLabel,
            AlbumReleaseDate = dto.Album.AlbumReleaseDate,
            PrimaryId = dto.Album.PrimaryId,
            ImageUrl = dto.Album.ImageUrl
        };

        TrackLinks = dto.TrackUrl?.Select(x => new TrackLink
        {
            Platform = TrackPlatformLinkService.TryMap(x.Platform, out Platform platform) ? platform.ToString() : x.Platform,
            Value = x.Value.ToString()
        }).ToList() ?? [];

        PlatformIds = platformIds;
        ContainerName = Environment.GetEnvironmentVariable("TRACK_CONTAINER_NAME") ?? "tracks";
        Path = string.Empty;
    }

    /// <summary>
    /// Gets the unique identifier for the track.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets the unique identifier of the user associated with this track.
    /// </summary>
    public Guid SpredUserId { get; private set; }

    /// <summary>
    /// Gets the partition bucket calculated from the user or track ID.
    /// </summary>
    public string Bucket => SpredUserId == Guid.Empty ? GuidShortener.GenerateBucketFromGuid(Id) : "00";

    /// <summary>
    /// Gets the uploaded form file for this track (if any).
    /// </summary>
    public IFormFile? FormFile { get; private set; }

    /// <summary>
    /// Gets or sets the relative blob or local path of the uploaded file.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets the primary identifier from the external platform (e.g., Spotify track ID).
    /// </summary>
    public string PrimaryId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the title of the track.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the description of the track.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the image URL of the track or album.
    /// </summary>
    public string ImageUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the language code of the track.
    /// </summary>
    public string LanguageCode { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the Chartmetrics internal track ID.
    /// </summary>
    public string ChartmetricsId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the SoundCharts internal track ID.
    /// </summary>
    public string SoundChartsId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets detailed audio features of the track.
    /// </summary>
    public AudioFeaturesDto Audio { get; private set; } = new();

    /// <summary>
    /// Gets the list of artists associated with the track.
    /// </summary>
    public List<Artist> Artists { get; private set; } = [];

    /// <summary>
    /// Gets the album information associated with the track.
    /// </summary>
    public Album? Album { get; private set; }

    /// <summary>
    /// Gets the collection of URLs linking to track pages across platforms.
    /// </summary>
    public List<TrackLink> TrackLinks { get; private set; } = [];

    /// <summary>
    /// Gets the collection of secondary platform IDs (platform, track ID pairs).
    /// </summary>
    public List<PlatformIdPair> PlatformIds { get; private set; } = [];

    /// <summary>
    /// Gets the popularity score of the track.
    /// </summary>
    public uint Popularity { get; private set; }

    /// <summary>
    /// Gets the publication date of the track.
    /// </summary>
    public DateTime Published { get; private set; }

    /// <summary>
    /// Gets the timestamp when the track was added.
    /// </summary>
    public DateTime AddedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the track was last updated.
    /// </summary>
    public DateTime UpdateAt { get; private set; }

    /// <summary>
    /// Gets the track source type (e.g., Direct, Spotify, Chartmetrics, etc.).
    /// </summary>
    public SourceType SourceType { get; private set; }

    /// <summary>
    /// Gets or sets the container name in which the file is stored.
    /// </summary>
    public string ContainerName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the upload status for two-phase commit.
    /// </summary>
    public UploadStatus Status { get; set; } = UploadStatus.Pending;

    /// <summary>
    /// Marks the track metadata status as Created.
    /// </summary>
    public void StatusCreated() => Status = UploadStatus.Created;

    /// <summary>
    /// Updates the image URL of the track after an upload or fetch.
    /// </summary>
    /// <param name="imageUrl">The new image URL.</param>
    public void UpdateImageUrl(string imageUrl)
    {
        if (!string.IsNullOrWhiteSpace(imageUrl))
            ImageUrl = imageUrl;
    }

    /// <summary>
    /// Updates technical track metadata using analyzed audio data.
    /// </summary>
    /// <param name="analyzeTrackDto">The analyzed track DTO containing audio properties.</param>
    public void UpdateByTrackAnalayze(AnalyzeTrackDto analyzeTrackDto)
    {
        Audio.Duration = analyzeTrackDto.Duration;
        Audio.Bitrate = analyzeTrackDto.Bitrate;
        Audio.SampleRate = analyzeTrackDto.SampleRate;
        Audio.Channels = analyzeTrackDto.Channels;
        Audio.Bpm = analyzeTrackDto.Bpm;
        Audio.Codec = analyzeTrackDto.Codec;
    }
}
