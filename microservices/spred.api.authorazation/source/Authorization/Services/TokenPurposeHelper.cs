using Authorization.Options;
using Authorization.Options.AuthenticationSchemes;
using Extensions.Configuration;

namespace Authorization.Services;

/// <summary>
/// Provides helper methods for working with token purposes and authentication types.
/// </summary>
public static class TokenPurposeHelper
{
    /// <summary>
    /// Returns the token purpose name combined with the authentication type.
    /// </summary>
    /// <param name="authType">The authentication type.</param>
    /// <returns>A string representing the token purpose with the authentication type.</returns>
    public static string GetPurposeName(AuthType authType)
    {
        return nameof(TokenPurposes.AccessToken) + authType;
    }

    /// <summary>
    /// Parses the authentication type from a given scheme string.
    /// </summary>
    /// <param name="scheme">The authentication scheme string.</param>
    /// <returns>The parsed <see cref="AuthType"/> or default if parsing fails.</returns>
    public static AuthType? GetAuthType(string scheme)
    {
        return Enum.TryParse(scheme, true, out AuthType authType) ? authType : null;
    }

    /// <summary>
    /// Returns the authentication scheme name based on the provided <see cref="AuthType"/>.
    /// </summary>
    /// <param name="authType"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string GetSchemeName(AuthType authType)
    {
        switch (authType)
        {
            case AuthType.Base:
                return "Base";
            case AuthType.Spotify:
                return SpotifyAuthenticationDefaults.AuthenticationScheme;
            case AuthType.Google:
                return GoogleAuthenticationDefaults.AuthenticationScheme;
            case AuthType.Yandex:
                return YandexAuthenticationDefaults.AuthenticationScheme;
            case AuthType.Microsoft:
                return MicrosoftManagementDefaults.AuthenticationScheme;
            default:
                throw new ArgumentOutOfRangeException(nameof(authType), authType, null);
        }
    } 
}
