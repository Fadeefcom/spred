namespace Authorization.Options.AuthenticationSchemes;

/// <summary>
/// Provides default values and constants for Yandex authentication integration.
/// </summary>
public static class YandexAuthenticationDefaults
{
    /// <summary>
    /// The default value used for CookieAuthenticationOptions.AuthenticationScheme
    /// </summary>
    public const string AuthenticationScheme = "Yandex";
    
    /// <summary>
    /// The default display name for Yandex authentication. Defaults to <c>Yandex</c>.
    /// </summary>
    public static readonly string DisplayName = "Yandex";

    /// <summary>
    /// The default endpoint used to perform Yandex authentication.
    /// </summary>
    public static readonly string AuthorizationEndpoint = "https://oauth.yandex.ru/authorize?response_type=code";

    /// <summary>
    /// The OAuth endpoint used to exchange access tokens.
    /// </summary>
    public static readonly string TokenEndpoint = "https://oauth.yandex.ru/token";

    /// <summary>
    /// The Yandex endpoint that is used to gather additional user information.
    /// </summary>
    public static readonly string UserInformationEndpoint = "https://login.yandex.ru/info?format=json";
}