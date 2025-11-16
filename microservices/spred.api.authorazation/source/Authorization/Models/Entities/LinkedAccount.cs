using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;
using Spred.Bus.Contracts;

namespace Authorization.Models.Entities;

/// <summary>
/// Represents an event associated with a linked account.
/// </summary>
/// <remarks>
/// This class serves as a domain event entity for linked accounts, capturing changes or actions
/// that occur related to a linked account, such as creation, updates, or other specific events.
/// Each event is uniquely identified and carries contextual and metadata information about the event.
/// </remarks>
public sealed class LinkedAccountEvent : IBaseEntity<Guid>
{
    /// <summary>
    /// Represents an event associated with a linked account in the authorization system.
    /// This entity is used to track and persist changes or actions related to linked accounts.
    /// </summary>
    public LinkedAccountEvent()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Represents the unique identifier for an account associated with the linked account event.
    /// This property is required and must be initialized during the creation of the <see cref="LinkedAccountEvent"/>.
    /// </summary>
    [PartitionKey(1)]
    public required string AccountId { get; init; }

    /// <summary>
    /// Gets or initializes the unique identifier of the user associated with the linked account event.
    /// </summary>
    /// <remarks>
    /// The <c>UserId</c> property represents the <see cref="Guid"/> used to identify a specific user.
    /// It is utilized to associate a linked account event with the corresponding user within the system.
    /// This property is required and must be set during initialization.
    /// </remarks>
    public required Guid? UserId { get; init; }

    /// <summary>
    /// Gets or sets the unique identifier used to correlate related events in the context of linked account operations.
    /// This property facilitates tracing and debugging by grouping events associated with a specific workflow or operation.
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Represents the platform associated with a linked account event.
    /// </summary>
    /// <remarks>
    /// This property is used to identify the specific platform (e.g., Spotify, Google, etc.)
    /// to which a linked account event pertains. It is critical for ensuring proper
    /// correlation between events and the respective platform.
    /// </remarks>
    [PartitionKey(0)]
    [JsonConverter(typeof(StringEnumConverter))]
    public required AccountPlatform Platform { get; init; }

    /// <summary>
    /// Represents the unique sequence number of an event within a linked account's timeline.
    /// </summary>
    /// <remarks>
    /// This property is used to maintain the order of events, ensuring they are processed in sequence.
    /// It is incremented automatically for each new event tied to a linked account.
    /// </remarks>
    public required long Sequence { get; init; }

    /// <summary>
    /// Represents the type of event associated with a linked account.
    /// </summary>
    /// <remarks>
    /// The <see cref="EventType"/> property is used to specify the nature of an event
    /// that has occurred in the lifecycle of a linked account. Examples of events include
    /// account creation, token issuance, submission of proof, and account verification.
    /// </remarks>
    [JsonConverter(typeof(StringEnumConverter))]
    public required LinkedAccountEventType EventType { get; init; }

    /// <summary>
    /// Gets the associated data or metadata for the linked account event.
    /// This property can be used to store detailed information about the event in the form
    /// of a JSON object. It may include additional context, configurations, or payload
    /// specifics related to the event being processed. The value can be null if no
    /// supplemental data is required for the event.
    /// </summary>
    public required JObject? Payload { get; init; }

    /// <summary>
    /// Gets the unique identifier for the linked account event.
    /// </summary>
    /// <remarks>
    /// This property is used to uniquely identify an instance of the LinkedAccountEvent entity.
    /// It is automatically initialized as a new GUID when the entity is created and cannot be modified.
    /// </remarks>
    public Guid Id { get; private set;  }

    /// <summary>
    /// Gets or sets the ETag associated with the entity.
    /// The ETag is used for concurrency control, preventing unintentional overwrites during updates.
    /// </summary>
    public string? ETag { get; private set; }

    /// Represents the timestamp for an event or state within the linked account system.
    /// It is a long value denoting the number of seconds since the Unix epoch (January 1, 1970).
    /// This property is primarily used for ordering events and tracking the time an event occurred
    /// in the linked account lifecycle.
    public long Timestamp { get; private set; }
}

/// <summary>
/// Represents the event triggered when an account is successfully linked
/// to a user or external platform. This event typically updates the
/// account state to verified and captures timestamps for when the account
/// was linked and verified, if not already set.
/// </summary>
public enum LinkedAccountEventType
{
    /// <summary>
    /// Represents the event type indicating that a new account has been created and linked successfully.
    /// This event is used to mark the creation and initial linkage of an account to a user or platform.
    /// </summary>
    AccountCreated,

    /// <summary>
    /// Represents the event type indicating that a token has been issued for a linked account.
    /// This event is typically used to record the issuance of authentication or API tokens
    /// associated with a linked account on a specified platform.
    /// </summary>
    TokenIssued,

    /// <summary>
    /// Specifies that a proof document or evidence was submitted for a linked account during the verification process.
    /// </summary>
    /// <remarks>
    /// The <c>ProofSubmitted</c> event is used to update the account's state to reflect the submission of necessary
    /// proof or documentation required for verifying the linked account.
    /// </remarks>
    ProofSubmitted,

    /// <summary>
    /// Represents an event indicating that a verification proof has been successfully attached
    /// to a linked account. This event occurs after the proof has been submitted and integrated
    /// into the account's state for further processing or validation.
    /// </summary>
    ProofAttached,

    /// <summary>
    /// Represents an event type indicating that the proof provided for account verification
    /// has been deemed invalid. This event is typically used when a verification process
    /// fails due to an issue with the submitted proof, such as incorrect or insufficient
    /// documentation.
    /// </summary>
    ProofInvalid,

    /// <summary>
    /// Represents the event type indicating that an account has been successfully verified.
    /// This event is triggered when the verification process for a linked account is completed,
    /// updating the account's status to Verified.
    /// </summary>
    AccountVerified,

    /// <summary>
    /// Represents an event indicating that a linked account has been unlinked from a user.
    /// </summary>
    AccountLinked,

    /// <summary>
    /// Represents the event type indicating that a previously linked account has been successfully unlinked.
    /// This event is used to signify the disconnection or removal of an association between an account and a user or platform.
    /// </summary>
    AccountUnlinked
}