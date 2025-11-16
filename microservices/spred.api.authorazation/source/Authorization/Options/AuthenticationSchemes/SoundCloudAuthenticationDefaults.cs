namespace Authorization.Options.AuthenticationSchemes;

public static class SoundCloudAuthenticationDefaults
{
    public const string AuthenticationScheme = "SoundCloud";

    public const string AuthorizationEndpoint = "https://api.soundcloud.com/connect";

    public const string TokenEndpoint = "https://api.soundcloud.com/oauth2/token";

    public const string UserInformationEndpoint = "https://api.soundcloud.com/me";
}