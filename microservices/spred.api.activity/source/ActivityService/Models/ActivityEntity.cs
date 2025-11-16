using System.Text.Json;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;
using Spred.Bus.Contracts;

namespace ActivityService.Models;

/// <summary>
/// Defines the consumer responsible for processing activity records and interacting with a data storage mechanism.
/// </summary>
/// <remarks>
/// This class implements functionality to consume activity-related messages, enabling the processing
/// of records and forwarding them to a Cosmos DB container or other storage system. It integrates
/// with message queue infrastructure to handle activity message flow within the system.
/// </remarks>
public sealed class ActivityEntity : IBaseEntity<Guid>
{
    /// Represents a unique identifier for the entity.
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the unique identifier of the actor user involved in the activity.
    /// </summary>
    /// <remarks>
    /// This property represents the user who has performed the action or is directly associated with the activity.
    /// It is used as a partition key in certain operations.
    /// </remarks>
    public required Guid ActorUserId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the other party involved in the activity, if applicable.
    /// </summary>
    /// <remarks>
    /// This property represents the ID of another user or participant associated with the specific activity.
    /// It can be null if there is no other party involved in the context of the activity.
    /// </remarks>
    public required Guid? OtherPartyUserId { get; init; }

    /// <summary>
    /// Gets or initializes the unique identifier of the user who owns the activity.
    /// </summary>
    /// <remarks>
    /// This property is required and represents the user who is the owner of the activity being recorded.
    /// It may be null in cases where ownership is not explicitly defined.
    /// </remarks>
    [PartitionKey(0)]
    public required Guid OwnerUserId { get; init; }

    /// <summary>
    /// Represents the type of object associated with an activity.
    /// This property specifies the object related to the activity, enabling further classification
    /// or processing of activities within the context of the system.
    /// </summary>
    public required string ObjectType { get; init; }

    /// Represents the unique identifier for an object associated with the activity entity.
    /// It is used as part of the partition key and serves as a reference to the specific object
    /// involved in the activity. This property is required for the initialization of an ActivityEntity.
    [PartitionKey(1)]
    public required Guid ObjectId { get; init; }

    /// <summary>
    /// Represents the action or operation performed in an activity.
    /// </summary>
    /// <remarks>
    /// This property defines the specific action or verb associated
    /// with an activity, such as "created", "updated", or "deleted".
    /// It is used to describe what operation was performed and is
    /// typically utilized in logging, feeds, or notification systems.
    /// </remarks>
    public required string Verb { get; init; }

    /// Represents the key used to identify the type or nature of an activity message.
    /// Typically used to determine the formatting and content of activity messages
    /// in components such as message formatters or consumers.
    public required string MessageKey { get; init; }

    /// <summary>
    /// Represents a dictionary of key-value pairs associated with an activity,
    /// containing additional contextual or custom information required to define
    /// or describe the activity in detail.
    /// </summary>
    /// <remarks>
    /// This property holds data that can be dynamically provided and consumed
    /// for various purposes, such as rendering or processing activity messages.
    /// The keys are strings and the values are objects, allowing storage of
    /// diverse types of information.
    /// </remarks>
    public required IDictionary<string, object?> Args { get; init; } = new Dictionary<string, object?>();

    /// Gets the state of the activity entity before the modification or action.
    /// Represents a serialized JSON structure capturing the prior state of the relevant object or data.
    /// Useful for creating activity logs or records that require tracking changes over time.
    public required JsonElement? Before { get; init; }

    /// Represents the state of an object or entity after a specific activity or operation has occurred.
    /// This property holds the updated state of the object in JSON format.
    /// It is used to track changes for activities, often in comparison with the `Before` property.
    public required JsonElement? After { get; init; }

    /// Gets the unique identifier used to correlate and track a specific request or operation
    /// across multiple systems or components within the application.
    /// This property is critical for ensuring observability, debugging, and tracing
    /// operations in distributed systems by maintaining a consistent identifier across logs.
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Gets the name or identifier of the service that generated the activity entity.
    /// </summary>
    /// <remarks>
    /// This property is used to track the source service responsible for creating the activity.
    /// </remarks>
    public required string Service { get; init; }

    /// <summary>
    /// Gets the importance level of the activity.
    /// </summary>
    /// <remarks>
    /// This property represents the significance or priority of the activity.
    /// The importance level is used to identify the relative weight or urgency of this activity
    /// within the context of the system's workflow or user interactions.
    /// </remarks>
    public required ActivityImportance Importance { get; init; }

    /// <summary>
    /// Gets the intended audience of the activity. This property specifies the group or entity
    /// for which the activity is relevant. The audience may influence how the activity is processed,
    /// displayed, or filtered.
    /// </summary>
    public required string Audience { get; init; }

    /// <summary>
    /// Represents the sequence number of the activity entity.
    /// This property is used to maintain the order of activities within a specific context,
    /// incrementing sequentially for each new activity.
    /// </summary>
    public required int Sequence { get; init; }

    /// <summary>
    /// Gets the tags associated with the activity.
    /// </summary>
    /// <remarks>
    /// This property represents a collection of tags that can be used to categorize or provide additional context
    /// about the activity. Each tag is a string and may represent different aspects or classifications of the activity.
    /// </remarks>
    public required string[] Tags { get; init; }

    /// Gets the timestamp for when the activity entity was created.
    /// This property is required and provides the precise date and time
    /// in Coordinated Universal Time (UTC) when the activity entry was generated.
    /// It is used to ensure accurate chronological ordering and auditing of activities.
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the ETag value associated with the entity, used for optimistic concurrency control.
    /// </summary>
    /// <remarks>
    /// The ETag property serves as a unique identifier for a specific version of the entity. It is typically
    /// used to ensure that updates to this entity are made to the correct version by comparing the ETag value
    /// during read and write operations. This helps prevent conflicts caused by concurrent modifications.
    /// </remarks>
    public string? ETag { get; private set; }

    /// <summary>
    /// Represents a timestamp indicating the point in time when an activity occurred or was recorded.
    /// </summary>
    /// <remarks>
    /// The <c>Timestamp</c> property is typically used for sorting and filtering activity records
    /// within the system. It provides a unique, monotonically increasing value useful for
    /// ordering events or determining their occurrence sequence.
    /// </remarks>
    public long Timestamp { get; private set; }
}