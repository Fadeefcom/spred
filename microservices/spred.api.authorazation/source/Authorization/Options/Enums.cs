namespace Authorization.Options
{
    /// <summary>
    /// Specifies the various roles available within the authorization system,
    /// typically used to define access levels and permissions for users.
    /// </summary>
    public enum RoleName
    {
        /// <summary>
        /// Represents the administrator role with the highest-level permissions,
        /// typically capable of managing all aspects of the system.
        /// </summary>
        Admin,

        /// <summary>
        /// Represents the administrative role specific to corporate-level operations
        /// with permissions tailored for managing corporate structures and processes.
        /// </summary>
        CorporateAdmin,

        /// <summary>
        /// Represents a corporate role within the system, typically associated
        /// with business-related permissions and access levels.
        /// </summary>
        Corporate,

        /// <summary>
        /// Represents a label role, potentially serving as a category or designation
        /// for specific user permissions or actions within the application.
        /// </summary>
        LabelManager,

        /// <summary>
        /// Specifies a role assigned to users responsible for curating and managing playlists within the system.
        /// </summary>
        PlaylistManager,

        /// <summary>
        /// Represents the role of an artist within the system, typically associated
        /// with users who create and manage their own creative content.
        /// </summary>
        Artist
    }

    /// <summary>
    /// Represents an error condition where the authentication or authorization token has expired,
    /// indicating that the token's validity period has ended and it can no longer be used for secure operations.
    /// </summary>
    public enum SecurityErrors
    {
        /// <summary>
        /// Indicates that the security token has been compromised or accessed by an unauthorized entity.
        /// </summary>
        TokenCompromised,


        /// <summary>
        /// Represents an error condition where the authentication or authorization token has expired,
        /// indicating that the token's validity period has ended and it can no longer be used for secure operations.
        /// </summary>
        TokenExpired,

        /// <summary>
        /// Represents a security error state where the token has been locked
        /// and cannot be used for authentication or authorization.
        /// </summary>
        TokenLocked,

        /// <summary>
        /// Indicates that the provided token is invalid, which may occur when the token is malformed, not recognized, or fails validation checks.
        /// </summary>
        TokenInvalid
    }

    /// <summary>
    /// Defines the types of authentication providers that can be used for external authentication.
    /// </summary>
    public enum AuthType
    {
        /// <summary>
        /// Represents the base authentication type, typically used for initial or unspecified authentication methods.
        /// </summary>
        Base,

        /// <summary>
        /// Represents the Spotify authentication type, typically associated with
        /// OAuth workflows supporting integration with Spotify's API and services.
        /// </summary>
        Spotify,

        /// <summary>
        /// Represents Google as an authentication provider used in the
        /// external authentication and authorization workflows.
        /// </summary>
        Google,

        /// <summary>
        /// Represents the Yandex authentication type, used for integrating OAuth-based
        /// authentication with Yandex's external authorization services.
        /// </summary>
        Yandex,
        
        /// <summary>
        /// Represents the Microsoft authentication type
        /// </summary>
        Microsoft,
    }
}
