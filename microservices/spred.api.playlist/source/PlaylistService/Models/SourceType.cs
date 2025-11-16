namespace PlaylistService.Models;

/// <summary>
/// Enum representing the different types of sources for a playlist.
/// </summary>
public enum SourceType
{
    /// <summary>
    /// Direct source type.
    /// </summary>
    Direct,

    /// <summary>
    /// Spotify source type.
    /// </summary>
    Spotify,

    /// <summary>
    /// SoundCloud source type.
    /// </summary>
    SoundCloud,
    
    /// <summary>
    /// Unspecified source type.
    /// </summary>
    Unspecified
}
