using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;
using Spred.Bus.Contracts;

namespace Authorization.Models.Entities;

/// <summary>
/// Base app user
/// </summary>
public class BaseUser : IBaseEntity<Guid>
{
    /// <summary>
    /// .ctor
    /// </summary>
    public BaseUser()
    {
        Id = Guid.NewGuid();
    }
    
    /// <summary>
    /// .ctor
    /// </summary>
    public BaseUser(string userName) : this()
    {
        UserName = userName;
    }
    
    /// <inheritdoc cref="IBaseEntity{TKey}"/>
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    [PartitionKey]
    public Guid Id { get; set; }
    
    /// <summary>
    /// The date and time when the user account was created.
    /// </summary>
    public DateTime Created { get; private set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The date and time when the user account was last updated.
    /// </summary>
    public DateTime Updated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User roles
    /// </summary>
    public HashSet<string> UserRoles { get; set; } = new (StringComparer.InvariantCultureIgnoreCase);
    
    /// <summary>
    /// User claims
    /// </summary>
    public Dictionary<string, string[]> UserClaims { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);

    /// <inheritdoc cref="IBaseEntity{TKey}"/>
    public string? ETag { get; private set; }
    
    /// <inheritdoc cref="IBaseEntity{TKey}"/>
    public long Timestamp { get; private set; }

    /// <summary>
    /// Gets or sets the user name for this user.
    /// </summary>
    [ProtectedPersonalData]
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the normalized user name for this user.
    /// </summary>
    public string? NormalizedUserName { get; set; }

    /// <summary>
    /// Gets or sets the email address for this user.
    /// </summary>
    [ProtectedPersonalData]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the normalized email address for this user.
    /// </summary>
    public string? NormalizedEmail { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if a user has confirmed their email address.
    /// </summary>
    /// <value>True if the email address has been confirmed, otherwise false.</value>
    [PersonalData]
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// Gets or sets a salted and hashed representation of the password for this user.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// A random value that must change whenever a users credentials change (password changed, login removed)
    /// </summary>
    public string? SecurityStamp { get; set; }

    /// <summary>
    /// Gets or sets a telephone number for the user.
    /// </summary>
    [ProtectedPersonalData]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if a user has confirmed their telephone address.
    /// </summary>
    /// <value>True if the telephone number has been confirmed, otherwise false.</value>
    [PersonalData]
    public bool PhoneNumberConfirmed { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if two factor authentication is enabled for this user.
    /// </summary>
    /// <value>True if 2fa is enabled, otherwise false.</value>
    [PersonalData]
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// Gets or sets the date and time, in UTC, when any user lockout ends.
    /// </summary>
    /// <remarks>
    /// A value in the past means the user is not locked out.
    /// </remarks>
    public DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if the user could be locked out.
    /// </summary>
    /// <value>True if the user could be locked out, otherwise false.</value>
    public  bool LockoutEnabled { get; set; }

    /// <summary>
    /// Gets or sets the number of failed login attempts for the current user.
    /// </summary>
    public  int AccessFailedCount { get; set; }
    
    /// <summary>
    /// User bio
    /// </summary>
    public string? Bio { get; set; }
    
    /// <summary>
    /// User location
    /// </summary>
    public string? Location { get; set; }
    
    /// <summary>
    /// User avatar url
    /// </summary>
    [Url]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Represents a collection of user accounts associated with this entity,
    /// where the key is a platform, and the value is additional account-specific data.
    /// </summary>
    public List<UserAccountRef> UserAccounts { get; set; } = new();

    /// <summary>
    /// Returns the username for this user.
    /// </summary>
    public override string ToString()
        => UserName ?? string.Empty;
}

/// <summary>
/// Represents a reference to a user account on an external platform.
/// </summary>
public sealed record UserAccountRef(
    [property: Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))] 
    AccountPlatform Platform,
    string AccountId,
    string ProfileUrl
);
