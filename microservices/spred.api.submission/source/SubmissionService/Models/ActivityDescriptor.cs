namespace SubmissionService.Models;

/// <summary>
/// Represents a high-level description of an activity to be logged
/// and persisted as part of the domain activity/audit system.
/// Used to construct <see cref="Spred.Bus.Contracts.ActivityRecord"/> instances
/// through activity pipeline behaviors or service helpers.
/// </summary>
/// <param name="Verb">
/// Action verb describing what happened (e.g., "created", "updated", "status_changed").
/// </param>
/// <param name="ObjectType">
/// Logical type of the object that was acted upon (e.g., "submission", "playlist").
/// </param>
/// <param name="ObjectId">
/// Unique identifier of the object that was acted upon.
/// </param>
/// <param name="MessageKey">
/// Localization or template key used to format a user-facing message
/// describing this activity.
/// </param>
/// <param name="Args">
/// Dynamic arguments to be injected into the localized or templated activity message.
/// </param>
/// <param name="Importance">
/// The relative importance or priority of the activity (e.g., Normal, Important, Critical).
/// </param>
/// <param name="OwnerUserId">
/// Identifier of the primary owner of the activity, typically the main user affected.
/// </param>
/// <param name="OtherPartyUserId">
/// Identifier of another user involved in the activity, if any (e.g., curator vs. artist).
/// </param>
/// <param name="Before">
/// Optional snapshot of the object state before the activity occurred.
/// </param>
/// <param name="After">
/// Optional snapshot of the object state after the activity occurred.
/// </param>
/// <param name="Tags">
/// Optional tags for categorizing or filtering the activity (e.g., "submission", "status_changed").
/// </param>
public sealed record ActivityDescriptor(
    string Verb,
    string ObjectType,
    Guid ObjectId,
    string MessageKey,
    IDictionary<string, object?> Args,
    Spred.Bus.Contracts.ActivityImportance Importance,
    Guid OwnerUserId,
    Guid? OtherPartyUserId,
    object? Before = null,
    object? After = null,
    IEnumerable<string>? Tags = null
);