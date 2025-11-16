namespace ActivityService.Models;

/// <summary>
/// Represents a single activity item formatted for the user feed.
/// </summary>
public sealed class ActivityFeedItem
{
    /// <summary>
    /// Unique identifier of the activity.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The UTC timestamp when the activity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The action verb (e.g., "created", "status_changed").
    /// </summary>
    public string Verb { get; init; } = default!;

    /// <summary>
    /// Logical type of the affected object (e.g., "submission", "user").
    /// </summary>
    public string ObjectType { get; init; } = default!;

    /// <summary>
    /// Identifier of the affected object.
    /// </summary>
    public Guid ObjectId { get; init; }

    /// <summary>
    /// Human-readable message describing the activity.
    /// </summary>
    public string Message { get; init; } = default!;
}