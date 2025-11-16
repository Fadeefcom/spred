namespace Authorization.Options.AuthenticationSchemes;

/// <summary>
/// Provides default values and constants used by Google authentication.
/// </summary>
public static class YoutubeAuthenticationDefaults
{
    /// <summary>
    /// The default scheme for Google authentication. Defaults to <c>Youtube</c>.
    /// </summary>
    public const string AuthenticationScheme = "Youtube";

    /// <summary>
    /// The default display name for Google authentication. Defaults to <c>Youtube</c>.
    /// </summary>
    public static readonly string DisplayName = "Youtube";

    /// <summary>
    /// The default endpoint used to perform Google authentication.
    /// </summary>
    /// <remarks>
    /// For more details about this endpoint, see <see href="https://developers.google.com/identity/protocols/oauth2/web-server#httprest"/>.
    /// </remarks>
    public static readonly string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

    /// <summary>
    /// The OAuth endpoint used to exchange access tokens.
    /// </summary>
    public static readonly string TokenEndpoint = "https://oauth2.googleapis.com/token";

    /// <summary>
    /// The Google endpoint that is used to gather additional user information.
    /// </summary>
    public static readonly string UserInformationEndpoint = "https://www.googleapis.com/oauth2/v3/userinfo";
}