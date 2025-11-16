using Newtonsoft.Json.Linq;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;
using Spred.Bus.Contracts;

namespace SubmissionService.Models.Entities;

/// <summary>
/// Represents an outbox event entity used to reliably publish domain events
/// related to submissions, following the outbox pattern.
/// </summary>
public sealed class OutboxEvent : IBaseEntity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxEvent"/> class
    /// with default values for identifier, type, state, and creation date.
    /// </summary>
    private OutboxEvent(string eventType, JObject payload)
    {
        EventType = eventType;
        Payload = payload;
        Id = Guid.NewGuid();
        Type = "Outbox";
        State = OutboxEventState.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the unique identifier of the outbox event.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the type label used to identify this entity in storage.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Gets the identifier of the submission associated with this event.
    /// </summary>
    public Guid SubmissionId { get; private set; }

    /// <summary>
    /// Gets the identifier of the catalog item linked to the event.
    /// </summary>
    [PartitionKey(1)] 
    public Guid CatalogItemId { get; private set; }

    /// <summary>
    /// Gets the identifier of the curator associated with the event.
    /// </summary>
    [PartitionKey(0)] 
    public Guid CuratorUserId { get; private set; }
    
    /// <summary>
    /// Track id
    /// </summary>
    public Guid TrackId { get; private set; }

    /// <summary>
    /// Gets the type of the event (e.g., created, status changed).
    /// </summary>
    public string EventType { get; private set; }

    /// <summary>
    /// Gets the serialized payload containing event data.
    /// </summary>
    public JObject Payload { get; private set; }

    /// <summary>
    /// Gets the current processing state of the event.
    /// </summary>
    public OutboxEventState State { get; private set; }

    /// <summary>
    /// Gets the date and time when the event was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the date and time when the event was published, if applicable.
    /// </summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <inheritdoc/>
    public string? ETag { get; private set; }

    /// <inheritdoc/>
    public long Timestamp { get; private set; }

    /// <summary>
    /// Creates an outbox event for a new submission.
    /// </summary>
    /// <param name="submission">The submission entity to create the event from.</param>
    /// <param name="correlationId">The correlation identifier for tracing.</param>
    /// <returns>A new <see cref="OutboxEvent"/> representing a submission creation event.</returns>
    public static OutboxEvent CreateSubmissionCreated(Submission submission, string correlationId)
    {
        return new OutboxEvent(nameof(SubmissionStatusChanged), JObject.FromObject(new SubmissionCreated(
            submission.Id, submission.ArtistId, submission.CuratorUserId,
            submission.CatalogItemId, submission.TrackId, submission.CreatedAt, correlationId)))
        {
            CuratorUserId = submission.CuratorUserId,
            CatalogItemId = submission.CatalogItemId,
            SubmissionId = submission.Id,
            CreatedAt = submission.CreatedAt,
            TrackId = submission.TrackId
        };
    }

    /// <summary>
    /// Creates an outbox event for a submission status change.
    /// </summary>
    /// <param name="submission">The submission entity to create the event from.</param>
    /// <param name="oldStatus">The previous submission status.</param>
    /// <param name="correlationId">The correlation identifier for tracing.</param>
    /// <returns>A new <see cref="OutboxEvent"/> representing a status change event.</returns>
    public static OutboxEvent CreateStatusChanged(Submission submission, SubmissionStatus oldStatus,  string correlationId)
    {
        return new OutboxEvent(nameof(SubmissionStatusChanged), JObject.FromObject(new SubmissionStatusChanged(submission.Id,
            oldStatus.ToString(), submission.Status.ToString(), submission.CuratorUserId, submission.UpdatedAt, correlationId)))
        {
            CuratorUserId = submission.CuratorUserId,
            CatalogItemId = submission.CatalogItemId,
            SubmissionId = submission.Id,
            CreatedAt = submission.CreatedAt,
            TrackId = submission.TrackId
        };
    }
}

/// <summary>
/// Represents the possible states of an outbox event.
/// </summary>
public enum OutboxEventState
{
    /// <summary>
    /// The event is pending and has not yet been published.
    /// </summary>
    Pending,

    /// <summary>
    /// The event has been successfully published.
    /// </summary>
    Published,

    /// <summary>
    /// The event failed to publish.
    /// </summary>
    Failed
}