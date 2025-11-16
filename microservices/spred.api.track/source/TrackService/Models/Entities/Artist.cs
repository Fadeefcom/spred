namespace TrackService.Models.Entities;

/// <summary>
/// Represents an artist with a primary identifier and a name.
/// </summary>
public sealed record Artist
{
    /// <summary>
    /// Gets the primary identifier of the artist.
    /// </summary>
    public required string PrimaryId { get; init; }

    /// <summary>
    /// Gets the name of the artist.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Image url 
    /// </summary>
    public string ImageUrl { get; init; } = string.Empty;
}
