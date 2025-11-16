using Microsoft.AspNetCore.Identity;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;

namespace Authorization.Models.Entities;

/// <summary>
/// Represents a role entity in the Identity system with additional metadata 
/// and Cosmos DB-specific configuration.
/// </summary>
public class BaseRole : IBaseEntity<Guid>
{
    /// <summary>
    /// Initializes a new instance of <see cref="IdentityRole{TKey}"/>.
    /// </summary>
    /// <param name="roleName">The role name.</param>
    public BaseRole(string roleName) : this()
    {
        Name = roleName;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IdentityRole{TKey}"/>.
    /// </summary>
    public BaseRole()
    {
        Id = Guid.NewGuid();
    }
    
    /// <inheritdoc />
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the entity tag (ETag) used for optimistic concurrency control in Cosmos DB.
    /// </summary>
    public string? ETag { get; private set;  }

    /// <summary>
    /// Gets the timestamp (in ticks) indicating the last update time in Cosmos DB.
    /// </summary>
    public long Timestamp { get;  private set;  }

    /// <summary>
    /// Gets or sets a dictionary of role claims, 
    /// where the key is the claim type and the value is the claim value.
    /// </summary>
    public Dictionary<string, string> RoleClaims { get; set; } = new();

    /// <summary>
    /// Gets or sets the normalized role name.
    /// Marked as the partition key for Cosmos DB storage.
    /// </summary>
    [PartitionKey]
    public string? NormalizedName { get; set; }

    /// <summary>
    /// Gets or sets the name for this role.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Returns the name of the role.
    /// </summary>
    /// <returns>The name of the role.</returns>
    public override string ToString()
    {
        return Name ?? string.Empty;
    }
}