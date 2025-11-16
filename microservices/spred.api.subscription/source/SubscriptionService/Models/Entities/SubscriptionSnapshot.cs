
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;

namespace SubscriptionService.Models.Entities;

/// <summary>
/// Represents a snapshot of a subscription, encapsulating relevant details and metadata
/// at a specific point in time.
/// </summary>
public class SubscriptionSnapshot : IBaseEntity<Guid>
{
    /// <summary>
    /// .ctor
    /// </summary>
    public SubscriptionSnapshot()
    {
        Id = Guid.NewGuid();
        Type = nameof(UserSubscriptionStatus);
    }

    /// <summary>
    /// Gets the type of the subscription or entity represented, usually utilized for
    /// distinguishing or identifying the nature of the object (e.g., UserSubscriptionStatus).
    /// </summary>
    public string Type { get; private set; }
    
    /// <inheritdoc />
    public long Timestamp { get; private set; }
    
    /// <inheritdoc />
    public Guid Id { get; private set; }
    
    /// <inheritdoc />
    public string? ETag { get; private set; }

    /// <summary>User this snapshot belongs to.</summary>
    [PartitionKey]
    public Guid UserId { get; init; }

    /// <summary>Last known status entity id (может быть Guid.Empty, если нет агрегата).</summary>
    public Guid StatusId { get; init; }

    /// <summary>Logical kind, e.g. "invoice:payment_succeeded".</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Source object id from Stripe (invoice id, session id, etc.).</summary>
    public string ExternalId { get; init; } = string.Empty;

    /// <summary>Raw JSON от Stripe (для последующего восстановления состояния).</summary>
    public string RawJson { get; init; } = string.Empty;
}