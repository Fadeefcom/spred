using System.Text.Json;

namespace AggregatorService.Models;

/// <summary>
/// Wrapper class for encapsulating raw playlist data retrieved from the Soundcharts API.
/// </summary>
public sealed class SoundchartsPlaylistWrapper
{
    /// <summary>
    /// Represents the raw JSON content retrieved for a playlist.
    /// </summary>
    public JsonElement Data { get; set; }
}

/// <summary>
/// Wrapper class designed to encapsulate track-related data retrieved from
/// Soundcharts, including raw JSON data and associated platform information.
/// </summary>
public sealed class SoundchartsTrackWrapper
{
    /// <summary>
    /// Represents the raw JSON element containing track information for soundcharts data processing.
    /// </summary>
    public JsonElement Data { get; set; }
    
}

/// <summary>
/// Wrapper class for holding raw track data associated with a playlist,
/// typically received from an external API like Chartmetrics.
/// </summary>
public class SpotifyTrackWrapper
{
    /// <summary>
    /// Raw JSON data representing playlist tracks.
    /// </summary>
    public JsonElement Data { get; set; }
}

/// <summary>
/// Wrapper class used to hold raw track-related data retrieved from the Chartmetrics API.
/// </summary>
public class ChartmetricsTrackWrapper
{
    /// <summary>
    /// Raw JSON data representing the track information from Chartmetrics.
    /// </summary>
    public JsonElement Data { get; set; }
}