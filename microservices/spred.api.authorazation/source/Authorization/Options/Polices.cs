namespace Authorization.Options;

/// <summary>
/// Represents a collection of global policy names used in authorization settings.
/// </summary>
/// <remarks>
/// This class contains constants defining various global role requirements
/// that are commonly used throughout the application's authorization policies.
/// These constants can be utilized to enforce role-based access control.
/// </remarks>
public class PolicyNameGlobal
{
    /// <summary>
    /// Represents the policy name for requiring administrative role authorization.
    /// This constant is used to define policies where access is restricted to users
    /// with administrator privileges.
    /// </summary>
    public const string RequireAdminRole = "RequireAdminRole";

    /// <summary>
    /// A constant representing the policy name for requiring the artist role in authorization checks.
    /// This is utilized to enforce the necessary access level for users with artist-related capabilities.
    /// </summary>
    public const string RequireArtistRole = "RequireStandartRole";

    /// <summary>
    /// Represents a policy name that requires the "Standard Role" for label-related access.
    /// This policy is utilized to enforce specific authorization rules based on the
    /// user's assigned role within the system.
    /// </summary>
    public const string RequireLabelRole = "RequireStandartRole";

    /// <summary>
    /// Defines the policy name for requiring the Playlister role.
    /// This policy ensures that only users with the Playlister role
    /// can access the resources or endpoints associated with this policy.
    /// </summary>
    public const string RequirePlaylisterRole = "RequireStandartRole";
}

/// <summary>
/// Represents a class containing constants for policy names used
/// in authorization configurations. This class inherits from
/// <see cref="PolicyNameGlobal"/> to extend with additional policy
/// definitions.
/// </summary>
public class PolicyName : PolicyNameGlobal
{
    /// <summary>
    /// A constant string representing a policy requirement for corporate admin role.
    /// This is used to enforce authorization rules where access is restricted
    /// to users with a corporate administrative role.
    /// </summary>
    public const string RequireCorporateAdminRole = "RequireCorporateAdminRole";

    /// <summary>
    /// Represents a policy name constant used to enforce authorization
    /// requiring the user to have a corporate role.
    /// </summary>
    public const string RequireCorporateRole = "RequireCorporateRole";

    /// <summary>
    /// Represents a policy name used to enforce elevated rights authorization within the system.
    /// This value is utilized to define specific access control policies for users that require higher-level permissions.
    /// </summary>
    public const string ElevatedRights = "ElevatedRights";
}