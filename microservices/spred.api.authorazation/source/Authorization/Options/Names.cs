namespace Authorization.Options;

/// <summary>
/// Provides constant values used throughout the application for identifying components and services.
/// </summary>
public static class Names
{
    /// Represents the name of the token provider used for generating user tokens.
    /// This is utilized within the Identity framework for operations such as
    /// user authentication, token generation, and validation processes.
    /// The name is referenced in services configuration and functionality such as
    /// `GenerateUserTokenAsync` to specify and differentiate the token provider.
    public const string UserTokenProvider = "UserTokenProvider";
}

/// <summary>
/// Provides constant header name values used in API requests or responses.
/// </summary>
public static class HeaderNames
{
    /// Represents the HTTP header used for passing authentication credentials.
    /// Typically used to provide tokens or credentials required for authorized communication
    /// between the client and server.
    public const string Authorization = "Authorization";
}
