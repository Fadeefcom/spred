using Refit;

namespace Authorization.Models.Dto;

/// <summary>
/// Spotify token request model.
/// </summary>
public class TokenRequest
{
    /// <summary>
    /// Gets or sets the grant type for the token request.
    /// </summary>
    [AliasAs("grant_type")]
    public required string GrantType { get; init; }

    /// <summary>
    /// Gets or sets the client ID for the token request.
    /// </summary>
    [AliasAs("client_id")]
    public required string ClientId { get; init; }

    /// <summary>
    /// Gets or sets the refresh token used to obtain a new access token.
    /// </summary>
    [AliasAs("refresh_token")]
    public required string RefreshToken { get; init; }
}
