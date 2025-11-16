using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;

namespace SubscriptionService.Models.Entities;

/// <summary>
/// Represents a user's subscription status record stored in the persistence layer.
/// This entity tracks the user's subscription lifecycle information, including current status,
/// billing period boundaries, and metadata for concurrency and partitioning.
/// </summary>
public class UserSubscriptionStatus : IBaseEntity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserSubscriptionStatus"/> class
    /// and automatically assigns a new unique identifier.
    /// </summary>
    public UserSubscriptionStatus()
    {
        Id = Guid.NewGuid();
        Type = nameof(UserSubscriptionStatus);
    }

    /// <summary>
    /// Gets the string representation of the entity type for the subscription status.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Gets the unique identifier of this subscription status entity.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the entity tag (ETag) used for optimistic concurrency control.
    /// </summary>
    public string? ETag { get; private set; }

    /// <summary>
    /// Gets the timestamp value used for internal versioning and ordering of updates.
    /// </summary>
    public long Timestamp { get; private set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user associated with this subscription status.
    /// Used as the partition key in Cosmos DB.
    /// </summary>
    [PartitionKey]
    public required Guid UserId { get; init; }

    /// <summary>
    /// Gets or sets the current subscription status of the user.
    /// Typical values include <c>active</c>, <c>inactive</c>, or <c>canceled</c>.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets or sets the reason associated with the user's subscription status update.
    /// This property provides contextual information about why a change in the subscription status occurred.
    /// </summary>
    public string LogicalState { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp marking the start of the current billing period.
    /// </summary>
    public DateTime? CurrentPeriodStart { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp marking the end of the current billing period.
    /// </summary>
    public DateTime? CurrentPeriodEnd { get; init; }
    
    /// <summary>
    /// Payment id.
    /// </summary>
    public string PaymentId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the identifier for the subscription associated with the user's subscription status.
    /// </summary>
    public string SubscriptionId { get; init; } = string.Empty;
}