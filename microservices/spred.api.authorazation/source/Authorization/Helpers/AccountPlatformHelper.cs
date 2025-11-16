using Authorization.Options.AuthenticationSchemes;
using Spred.Bus.Contracts;

namespace Authorization.Helpers;

/// <summary>
/// Helper that normalizes platform identifiers and authentication schemes to <see cref="AccountPlatform"/> values,
/// and exposes a reverse mapping for canonical string keys.
/// </summary>
/// <remarks>
/// <para><see cref="PlatformMap"/> uses <see cref="StringComparer.OrdinalIgnoreCase"/> to accept case-insensitive keys
/// such as "spotify", "Spotify", or "SPOTIFY".</para>
/// <para><see cref="ReverseMap"/> is built from <see cref="PlatformMap"/> and provides the canonical lowercase slug
/// (e.g., "youtube-music") for a given <see cref="AccountPlatform"/>.</para>
/// <para><see cref="PlatformSchemeMap"/> maps concrete authentication scheme names to <see cref="AccountPlatform"/> values,
/// allowing translation from middleware/auth configuration to a platform enum.</para>
/// </remarks>
public static class AccountPlatformHelper
{
    /// <summary>
    /// Maps human-readable platform slugs to <see cref="AccountPlatform"/> values.
    /// </summary>
    /// <example>
    /// "spotify" → <see cref="AccountPlatform.Spotify"/>,
    /// "apple-music" → <see cref="AccountPlatform.AppleMusic"/>,
    /// "deezer" → <see cref="AccountPlatform.Deezer"/>,
    /// "soundcloud" → <see cref="AccountPlatform.SoundCloud"/>,
    /// "youtube-music" → <see cref="AccountPlatform.YouTubeMusic"/>.
    /// </example>
    /// <remarks>
    /// The dictionary is case-insensitive via <see cref="StringComparer.OrdinalIgnoreCase"/>.
    /// </remarks>
    public static readonly Dictionary<string, AccountPlatform> PlatformMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["spotify"] = AccountPlatform.Spotify,
            ["apple-music"] = AccountPlatform.AppleMusic,
            ["deezer"] = AccountPlatform.Deezer,
            ["soundcloud"] = AccountPlatform.SoundCloud,
            ["youtube-music"] = AccountPlatform.YouTubeMusic
        };
    
    /// <summary>
    /// Reverse map from <see cref="AccountPlatform"/> to its canonical slug representation as used in <see cref="PlatformMap"/>.
    /// </summary>
    /// <remarks>
    /// The value for each key is the normalized, canonical key (e.g., "youtube-music") that appears in <see cref="PlatformMap"/>.
    /// </remarks>
    public static readonly Dictionary<AccountPlatform, string> ReverseMap =
        PlatformMap.ToDictionary(kv => kv.Value, kv => kv.Key);
    
    /// <summary>
    /// Maps concrete authentication scheme names to <see cref="AccountPlatform"/> values.
    /// </summary>
    /// <remarks>
    /// Use this to translate an incoming authentication scheme (e.g., from challenge/handler configuration)
    /// to the corresponding <see cref="AccountPlatform"/>.
    /// </remarks>
    public static readonly Dictionary<string, AccountPlatform> PlatformSchemeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [YoutubeAuthenticationDefaults.AuthenticationScheme] = AccountPlatform.YouTubeMusic,
        [SoundCloudAuthenticationDefaults.AuthenticationScheme] = AccountPlatform.SoundCloud,
    };
}