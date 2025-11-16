using System.Text.Json.Serialization;

namespace AggregatorService.Models.Dto;

/// <summary>
/// Represents the response returned by the Spotify Accounts service when
/// requesting an access token via the Client Credentials flow.
/// </summary>
public sealed class TokenResponse
{
    /// <summary>
    /// Gets the access token issued by the Spotify Accounts service.
    /// This token is required for making authorized requests to the Spotify Web API.
    /// </summary>
    [JsonPropertyName("access_token")] 
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of the returned token. 
    /// For Spotify Web API, this is always "Bearer".
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the lifetime of the access token, in seconds. 
    /// After this duration, a new token must be requested.
    /// </summary>
    [JsonPropertyName("expires_in")] 
    public int ExpiresIn { get; init; }
}