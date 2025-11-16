namespace SubscriptionService.Models;

/// <summary>
/// Represents a request to initiate a Stripe checkout session for a specific subscription plan.
/// </summary>
/// <param name="Plan">
/// The identifier or name of the subscription plan selected by the user.
/// </param>
public record CheckoutRequest(string Plan);

/// <summary>
/// Represents a request to cancel an existing user subscription.
/// </summary>
public record CancelSubscriptionRequest(string? Reason);

/// <summary>
/// Represents a request to process a refund for a subscription.
/// </summary>
/// <param name="Reason">
/// The optional reason provided by the user for requesting the refund.
/// </param>
public record RefundRequest(string? Reason);

/// <summary>
/// Represents the result of an atomic save operation, typically for status and snapshot data.
/// </summary>
/// <param name="StatusSaved">
/// Indicates whether the status was successfully saved.
/// </param>
/// <param name="SnapshotSaved">
/// Indicates whether the snapshot was successfully saved.
/// </param>
/// <param name="StatusEtag">
/// The ETag associated with the saved status, if applicable.
/// </param>
/// <param name="SnapshotEtag">
/// The ETag associated with the saved snapshot, if applicable.
/// </param>
/// <param name="HttpStatus">
/// The HTTP status code resulting from the save operation.
/// </param>
/// <param name="Error">
/// An error message, if the save operation failed.
/// </param>
public sealed record AtomicSaveResult(
    bool StatusSaved,
    bool SnapshotSaved,
    string? StatusEtag,
    string? SnapshotEtag,
    System.Net.HttpStatusCode HttpStatus,
    string? Error
);