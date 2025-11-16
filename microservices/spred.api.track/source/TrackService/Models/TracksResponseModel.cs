using Spred.Bus.Contracts;
using Spred.Bus.DTOs;
using TrackService.Models.DTOs;

namespace TrackService.Models;

/// <summary>
/// Represents the response model for getting tracks.
/// </summary>
public sealed record TracksResponseModel
{
    /// <summary>
    /// Gets the total number of tracks.
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// Gets the list of track data transfer objects.
    /// </summary>
    public List<PrivateTrackDto>? Tracks { get; init; }
}
