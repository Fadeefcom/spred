using System.ComponentModel.DataAnnotations;

namespace TrackService.Models.DTOs;

/// <summary>
/// Represents a command to create a new track.
/// </summary>
public sealed record TrackCreate
{
    /// <summary>
    /// Gets or sets the title of the track.
    /// </summary>
    [RegularExpression(@"^[\p{L}\p{N}\s.,!?'\-]{3,100}$", ErrorMessage = "Title must be between 3 and 100 characters and contain only letters, numbers, spaces, and basic punctuation.")]
    public required string Title { get; init; }

    /// <summary>
    /// Gets or sets the description of the track.
    /// </summary>
    [RegularExpression(@"^[\p{L}\p{N}\s.,!?'\-]{0,500}$", ErrorMessage = "Description can only contain letters, numbers, spaces, and basic punctuation (max 500 characters).")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the URL of the track.
    /// </summary>
    [Url(ErrorMessage = "Invalid URL format.")]
    public string? TrackUrl { get; init; }
}
