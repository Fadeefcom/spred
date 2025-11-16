using SubscriptionService.Models;
using SubscriptionService.Models.Entities;

namespace SubscriptionService.Abstractions;

/// <summary>
/// Defines operations for retrieving and updating user subscription status information in persistent storage.
/// </summary>
public interface ISubscriptionStateStore
{
    /// <summary>
    /// Asynchronously retrieves the latest subscription status for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose subscription status is being requested.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the current subscription status, or <c>null</c> if not found.
    /// </returns>
    Task<bool?> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously updates or stores the subscription status for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose subscription status is being updated.</param>
    /// <param name="paymentId">Payment id.</param>
    /// <param name="isActive">The new subscription status to persist.</param>
    /// <param name="periodEnd"></param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <param name="subscriptionId"></param>
    /// <param name="logicalState"></param>
    /// <param name="periodStart"></param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task<Guid?> SetStatusAsync(Guid userId, string paymentId, bool isActive,string subscriptionId, string? logicalState, DateTime? periodStart = null,
        DateTime? periodEnd = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves detailed subscription status information for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose subscription details are being requested.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the user's subscription status details, or <c>null</c> if not found.
    /// </returns>
    Task<UserSubscriptionStatus?> GetDetailsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously saves a snapshot of the subscription state for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user associated with the subscription state.</param>
    /// <param name="statusId">The unique identifier of the status being saved.</param>
    /// <param name="kind">The type or category of the subscription state.</param>
    /// <param name="id">A unique identifier used to distinguish the snapshot instance.</param>
    /// <param name="rawJson">The raw JSON representation of the subscription state snapshot.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    Task<Guid?> SaveSnapshotAsync(Guid userId, Guid statusId, string kind, string id, string rawJson,
        CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously saves the user's subscription status and snapshot atomically.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose subscription status is being updated.</param>
    /// <param name="status">The subscription status information to be saved for the user.</param>
    /// <param name="kind">The type or classification of the subscription snapshot being saved.</param>
    /// <param name="externalId">An external identifier associated with the subscription or snapshot.</param>
    /// <param name="rawJson">The raw JSON data representing additional details about the subscription or snapshot.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an <see cref="AtomicSaveResult"/> indicating the status of the save operation.
    /// </returns>
    public Task<AtomicSaveResult> SaveAtomicAsync(
        Guid userId,
        UserSubscriptionStatus status,
        string kind, string externalId, string rawJson,
        CancellationToken cancellationToken = default);
}
