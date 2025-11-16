using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Spred.Bus.Contracts;

namespace Authorization.Models.Entities;

/// Represents the state of a linked account, including its status, associated user, platform, and other metadata.
/// This class is immutable and designed to capture the state of a user's linked account as it changes over time.
/// Events can be applied to modify the state using the rehydration method.
public sealed class LinkedAccountState
{
    /// Represents the unique identifier for a linked account within the system.
    /// This property is required and used to associate the linked account with an external system or user.
    /// It is immutable and provided during the initialization of the object.
    public required string AccountId { get; init; }

    /// <summary>
    /// Represents the unique identifier for a user associated with a linked account.
    /// </summary>
    /// <remarks>
    /// This property is required and is used to distinguish the user linked to a specific account.
    /// It is an immutable GUID value set during initialization.
    /// </remarks>
    public required Guid UserId { get; init; }

    /// <summary>
    /// Specifies the platform associated with the linked account.
    /// </summary>
    /// <remarks>
    /// The platform denotes the underlying service or provider (e.g., social media platform)
    /// where the linked account is registered. It is a required property and its value
    /// is represented as an <see cref="AccountPlatform"/> enumeration.
    /// </remarks>
    [JsonConverter(typeof(StringEnumConverter))] 
    public required AccountPlatform Platform { get; init; }

    /// <summary>
    /// Represents the current status of the linked account within the state machine.
    /// </summary>
    /// <remarks>
    /// The status is managed by applying successive events to the linked account's state.
    /// Possible values for this property are defined in the <see cref="AccountStatus"/> enumeration,
    /// including Pending, TokenIssued, ProofSubmitted, Verified, Error, and Deleted.
    /// The status changes based on events such as account creation, proof submission,
    /// and verification or deletion of the linked account.
    /// </remarks>
    /// <value>
    /// A value of type <see cref="AccountStatus"/> representing the current state of the account.
    /// </value>
    [JsonConverter(typeof(StringEnumConverter))] 
    public AccountStatus Status { get; private set; } = AccountStatus.Pending;

    /// <summary>
    /// Represents the unique identifier used to correlate events and processes
    /// related to the lifecycle of a linked account in the system.
    /// </summary>
    /// <remarks>
    /// This identifier is primarily updated during state transitions triggered by
    /// specific events, such as account creation, verification, or linkage. It helps ensure
    /// reliable tracking and association of related operations.
    /// </remarks>
    /// <example>
    /// Used internally by components like event handlers for maintaining consistency
    /// across the execution of domain events.
    /// </example>
    public Guid CorrelationId { get; private set; }

    /// Gets the proof associated with the linked account.
    /// This property represents additional verification data that may be submitted
    /// or attached during the life cycle of the account linking process.
    /// It may contain a null value if no proof data is available or has been cleared.
    /// The value is set during specific events such as `ProofSubmitted` or `ProofAttached`.
    /// This data is serialized as a JSON object using Newtonsoft.Json.
    public JObject? Proof { get; private set; }

    /// Represents the date and time when the linked account was created.
    /// The value is set when specific account-related events, such as account creation or linking, occur.
    /// This property is readonly and is initialized based on the corresponding event's timestamp.
    public DateTimeOffset CreatedAt { get; private set; }

    /// Represents the date and time when the linked account was verified, if applicable.
    /// This property is updated when the account undergoes and completes the verification process,
    /// typically upon receiving specific events such as `AccountVerified` or `AccountLinked`.
    /// A null value indicates that the account has not been verified yet.
    public DateTimeOffset? VerifiedAt { get; private set; }

    /// <summary>
    /// Represents the sequential order of events applied to the linked account state.
    /// </summary>
    /// <remarks>
    /// The <c>Sequence</c> property is used for tracking and enforcing the order of events related to a linked account.
    /// This ensures consistency in the state changes of the account by maintaining an incrementing value for every event applied.
    /// Any event with a sequence number less than or equal to the current <c>Sequence</c> value is ignored.
    /// </remarks>
    public long Sequence { get; private set; } = -1;

    /// <summary>
    /// Gets the last event type associated with the linked account.
    /// </summary>
    /// <remarks>
    /// LastEventType indicates the most recent event that occurred for the linked account,
    /// represented as a value from the <see cref="LinkedAccountEventType"/> enumeration.
    /// Common events include actions such as account creation, token issuance,
    /// proof submission, and account verification.
    /// </remarks>
    public LinkedAccountEventType LastEventType { get; private set; }

    /// <summary>
    /// Applies a given <see cref="LinkedAccountEvent"/> to change the state of the current LinkedAccountState instance.
    /// </summary>
    /// <param name="e">The <see cref="LinkedAccountEvent"/> to apply that contains event data, sequence, and event type information.</param>
    private void Apply(LinkedAccountEvent e)
    {
        if (e.Sequence <= Sequence) return;

        switch (e.EventType)
        {
            case LinkedAccountEventType.AccountCreated:
                Sequence = e.Sequence;
                Status = AccountStatus.Pending;
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(e.Timestamp);
                CorrelationId = e.CorrelationId;
                Proof = null;
                VerifiedAt = null;
                break;

            case LinkedAccountEventType.TokenIssued:
                Sequence = e.Sequence;
                Status = AccountStatus.TokenIssued;
                break;

            case LinkedAccountEventType.ProofSubmitted:
                Sequence = e.Sequence;
                Status = AccountStatus.ProofSubmitted;
                Proof = e.Payload;
                break;

            case LinkedAccountEventType.ProofAttached:
                Sequence = e.Sequence;
                Proof = e.Payload;
                break;

            case LinkedAccountEventType.ProofInvalid:
                Sequence = e.Sequence;
                Status = AccountStatus.Error;
                break;

            case LinkedAccountEventType.AccountVerified:
                Sequence = e.Sequence;
                Status = AccountStatus.Verified;
                VerifiedAt = DateTimeOffset.FromUnixTimeSeconds(e.Timestamp);
                break;

            case LinkedAccountEventType.AccountLinked:
                Sequence = e.Sequence;
                Status = AccountStatus.Verified;
                if(CreatedAt == default)
                    CreatedAt = DateTimeOffset.FromUnixTimeSeconds(e.Timestamp);
                if (VerifiedAt is null)
                    VerifiedAt = DateTimeOffset.FromUnixTimeSeconds(e.Timestamp);
                break;

            case LinkedAccountEventType.AccountUnlinked:
                Sequence = e.Sequence;
                Status = AccountStatus.Deleted;
                break;
        }
        
        LastEventType = e.EventType;
    }

    /// <summary>
    /// Rebuilds the state of a linked account from a seed state and a collection of events.
    /// </summary>
    /// <param name="seed">The initial state of the linked account to be rehydrated.</param>
    /// <param name="events">The collection of linked account events to apply to the seed state, ordered by sequence and timestamp.</param>
    /// <returns>The fully rehydrated <see cref="LinkedAccountState"/> after applying all events in order.</returns>
    public static LinkedAccountState Rehydrate(LinkedAccountState seed, IEnumerable<LinkedAccountEvent> events)
    {
        foreach (var e in events.OrderBy(x => x.Sequence).ThenBy(x => x.Timestamp))
            seed.Apply(e);
        return seed;
    }
}

/// <summary>
/// Represents a state of an account where an error has occurred.
/// </summary>
/// <remarks>
/// This status indicates that the account has encountered an issue,
/// such as invalid proof being submitted during the account verification process.
/// It is typically set in response to specific events, such as <see cref="LinkedAccountEventType.ProofInvalid"/>.
/// </remarks>
[JsonConverter(typeof(StringEnumConverter))]
public enum AccountStatus
{
    /// <summary>
    /// Represents the initial state of an account when it is created but not yet verified or processed.
    /// </summary>
    /// <remarks>
    /// The Pending status indicates that
    Pending,

    /// <summary>
    /// Represents the state of an account where a token has been successfully issued.
    /// This status indicates that a token linked to the account has been generated
    /// and is available for further processes or actions in the account lifecycle.
    /// Typically transitions from the Pending state.
    /// </summary>
    TokenIssued,

    /// <summary>
    /// Indicates that proof of ownership or verification has been submitted for the associated account.
    /// </summary>
    /// <remarks>
    /// The "ProofSubmitted" status typically occurs after a user has uploaded or provided the required
    /// evidence to verify ownership of their account in the system. The proof may then await further
    /// processing or verification.
    /// </remarks>
    ProofSubmitted,

    /// <summary>
    /// Represents the state of an account that has been verified.
    /// </summary>
    Verified,

    /// <summary>
    /// Represents a state of an account where an error has occurred.
    /// </summary>
    Error,

    /// <summary>
    /// Indicates that the account has been permanently removed or deactivated.
    /// </summary>
    Deleted
}
