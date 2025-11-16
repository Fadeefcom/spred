namespace Authorization.Models.Dto;

/// <summary>
/// Represents a request to queue user playlists.
/// </summary>
public sealed record QueueUserPlaylistsRequest
{
    /// <summary>
    /// Gets the Spotify ClientID associated with the request.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Spred user ID if assigned.
    /// </summary>
    public Guid? SpredUserId { get; set; }

    /// <summary>
    /// Submition urls
    /// </summary>
    public Dictionary<string, string> SubmitUrls { get; set; } = [];
}