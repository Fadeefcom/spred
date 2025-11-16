using System.ComponentModel.DataAnnotations;
using TrackService.Models.Entities;

namespace TrackService.Models.DTOs;

/// <summary>
/// Represents a data transfer object for a private track, containing detailed metadata.
/// </summary>
public sealed class PrivateTrackDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the private track.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the Spred user associated with the track.
    /// </summary>
    public Guid SpredUserId { get; set; }

    /// <summary>
    /// Gets or sets the title of the track.
    /// </summary>
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Provides a description for the associated entity or item.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the duration of the track.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the bitrate of the track.
    /// </summary>
    public uint Bitrate { get; set; }

    /// <summary>
    /// Gets or sets the sample rate of the track.
    /// </summary>
    public uint SampleRate { get; set; }

    /// <summary>
    /// Gets or sets the number of audio channels.
    /// </summary>
    public uint Channels { get; set; }

    /// <summary>
    /// Gets or sets the codec of the audio track.
    /// </summary>
    public string Codec { get; set; } = default!;

    /// <summary>
    /// Gets or sets the beats per minute (BPM) of the track.
    /// </summary>
    public ushort Bpm { get; set; }

    /// <summary>
    /// Gets or sets the genre of the track.
    /// </summary>
    public string Genre { get; set; } = default!;

    /// <summary>
    /// Gets or sets the energy level of the track.
    /// </summary>
    public double Energy { get; set; }

    /// <summary>
    /// Represents the musical valence of a track, indicating the positivity or happiness of the track's tone.
    /// </summary>
    public double Valence { get; set; }

    /// <summary>
    /// Represents the popularity metric of a track, indicating its overall acclaim or reception.
    /// </summary>
    public uint Popularity { get; set; }

    /// <summary>
    /// Gets or sets the URL of the image associated with the track.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Represents a collection of URLs associated with the track for different platforms.
    /// </summary>
    public IList<TrackLink> TrackUrl { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the track was published.
    /// </summary>
    public DateTime Published { get; set; }

    /// <summary>
    /// Indicates the date and time when the private track was added to the system or collection.
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the track was last updated.
    /// </summary>
    public DateTime UpdateAt { get; set; }

    /// <summary>
    /// Gets or sets the collection of artists associated with the track.
    /// </summary>
    public List<PrivateArtistDto> Artists { get; set; } = new();

    /// <summary>
    /// Gets or sets the album information associated with the track.
    /// </summary>
    public PrivateAlbumDto? Album { get; set; }
}

/// <summary>
/// Represents a data transfer object for a private artist.
/// </summary>
public sealed record PrivateArtistDto(string Name, string ImageUrl);

/// <summary>
/// Represents a data transfer object for a private album.
/// </summary>
public sealed record PrivateAlbumDto(
    string AlbumName,
    string AlbumLabel,
    string AlbumReleaseDate,
    string ImageUrl
);
