namespace Authorization.Options.AuthenticationSchemes;

/// <summary>
/// Provides default values used by the Spotify authentication handler.
/// </summary>
public static class SpotifyAuthenticationDefaults
{
    /// <summary>
    /// The default value used for CookieAuthenticationOptions.AuthenticationScheme
    /// </summary>
    public const string AuthenticationScheme = "Spotify";

    /// <summary>
    /// The default display name for Spotify authentication. Defaults to <c>Spotify</c>.
    /// </summary>
    public static readonly string DisplayName = "Spotify";

    /// <summary>
    /// The default endpoint used to perform Spotify authentication.
    /// </summary>
    public static readonly string AuthorizationEndpoint = "https://accounts.spotify.com/authorize?";

    /// <summary>
    /// The OAuth endpoint used to exchange access tokens.
    /// </summary>
    public static readonly string TokenEndpoint = "https://accounts.spotify.com/api/token";

    /// <summary>
    /// The Spotify endpoint that is used to gather additional user information.
    /// </summary>
    public static readonly string UserInformationEndpoint = "https://api.spotify.com/v1/me";
}
