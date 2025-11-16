namespace TrackService.Models.Entities;

/// <summary>
/// Represents a music album with its primary identifier, name, and release date.
/// </summary>

public sealed record Album
{    
    /// <summary>
    /// Gets the primary identifier of the album.
    /// </summary>
    public required string PrimaryId { get; init; }

    /// <summary>
    /// Gets the name of the album.
    /// </summary>
    public string AlbumName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the release date of the album.
    /// </summary>
    public string AlbumReleaseDate { get; init; } = string.Empty;
    
    /// <summary>
    /// Album label
    /// </summary>
    public string AlbumLabel { get; init; } = string.Empty;
    
    /// <summary>
    /// Image url
    /// </summary>
    public string ImageUrl { get; init; } = string.Empty;
}
