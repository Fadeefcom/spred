using Spred.Bus.DTOs;

namespace TrackService.Models.DTOs;

/// <summary>
/// Public track dto
/// </summary>
public class PublicTrackDto
{
    /// <summary>
    /// Gets the title of the track.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the URL of the track.
    /// </summary>
    public string? TrackUrl { get; set; }

    /// <summary>
    /// Gets the list of artists associated with the track.
    /// </summary>
    public List<ArtistDto> Artists { get; init; } = [];
}