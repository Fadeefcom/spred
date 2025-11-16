using Authorization.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json.Linq;
using Spred.Bus.Contracts;

namespace Authorization.Abstractions;

/// <summary>
/// Represents an interface for handling events related to linked accounts.
/// Provides methods to retrieve the current account state, append new events,
/// and unlink accounts.
/// </summary>
public interface ILinkedAccountEventStore
{
    /// Retrieves the current state of a linked account based on the provided parameters.
    /// <param name="accountId">The unique identifier of the linked account.</param>
    /// <param name="platform">The platform associated with the linked account (e.g., Spotify, AppleMusic).</param>
    /// <param name="userId">The unique identifier of the user associated with the account.</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation.</param>
    /// <returns>The current state of the linked account represented as a <see cref="LinkedAccountState"/> object, or null if no state could be determined.</returns>
    public Task<LinkedAccountState?> GetCurrentState(string accountId, AccountPlatform platform, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Appends a new event to the linked account event store for a specified account.
    /// </summary>
    /// <param name="accountId">
    /// The unique identifier for the external account being linked.
    /// </param>
    /// <param name="userId">
    /// The unique identifier of the user associated with the external account.
    /// </param>
    /// <param name="platform">
    /// The platform associated with the account (e.g., Spotify, AppleMusic).
    /// </param>
    /// <param name="type">
    /// The type of event to append to the account event store (e.g., AccountCreated, AccountVerified).
    /// </param>
    /// <param name="payload">
    /// An optional JSON object containing additional details related to the event.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to signal operation cancellation.
    /// </param>
    /// <returns>
    /// A task that represents the result of the operation. The task result contains an <see cref="IdentityResult"/>
    /// indicating whether the operation was successful.
    /// </returns>
    public Task<IdentityResult> AppendAsync(string accountId, Guid userId, AccountPlatform platform, LinkedAccountEventType type, JObject? payload,  CancellationToken cancellationToken);

    /// <summary>
    /// Unlinks an account from a user's profile.
    /// </summary>
    /// <param name="accountId">The ID of the account to be unlinked.</param>
    /// <param name="userId">The unique identifier of the user associated with the account.</param>
    /// <param name="platform">The platform associated with the account.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to observe cancellation requests.</param>
    /// <returns>
    /// Returns an <see cref="IdentityResult"/> indicating the success or failure of the unlink operation.
    /// </returns>
    public Task<IdentityResult> UnlinkAsync(string accountId, Guid userId, AccountPlatform platform, CancellationToken cancellationToken);
}