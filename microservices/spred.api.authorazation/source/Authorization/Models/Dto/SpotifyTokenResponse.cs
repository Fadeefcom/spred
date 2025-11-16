using System.Text.Json.Serialization;

namespace Authorization.Models.Dto;

/// <summary>
/// Represents the response from Spotify's token endpoint.
/// </summary>
public sealed record SpotifyTokenResponse
{
    /// <summary>
    /// Gets or sets the access token issued by Spotify.
    /// </summary>
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the type of token issued. Typically "Bearer".
    /// </summary>
    [JsonPropertyName("token_type")]
    public required string TokenType { get; set; }

    /// <summary>
    /// Gets or sets the duration in seconds until the token expires.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; set; }
}
