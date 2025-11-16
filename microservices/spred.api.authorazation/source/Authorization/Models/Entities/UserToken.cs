using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;

namespace Authorization.Models.Entities;

/// <summary>
/// User token entity
/// </summary>
public class UserToken : IBaseEntity<Guid>
{
    /// <summary>
    /// .ctor
    /// </summary>
    public UserToken()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// UserID
    /// </summary>
    [PartitionKey(0)]
    public Guid UserId { get; set; }

    /// <summary>
    /// Login provider
    /// </summary>
    [PartitionKey(1)]
    public required string LoginProvider { get; init; }

    /// <summary>
    /// Token name
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Token value
    /// </summary>
    public required string? Value { get; init; }
    
    /// <inheritdoc />
    public Guid Id { get;  private set;  }
    
    /// <inheritdoc />
    public string? ETag { get;  private set;  }
    
    /// <inheritdoc />
    public long Timestamp { get;  private set;  }
}