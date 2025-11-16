using Authorization.Models.Entities;
using Authorization.Options;
using Microsoft.AspNetCore.Identity;

namespace Authorization.Abstractions;

/// <summary>
/// Extends the default ASP.NET Core Identity <see cref="IUserStore{TUser}"/> 
/// with additional methods for managing external logins, feedback, and notifications.
/// </summary>
public interface IUserPlusStore : IUserRoleStore<BaseUser>, IUserAuthenticationTokenStore<BaseUser>
{
    /// <summary>
    /// Creates a new <paramref name="user"/> in the user store and associates it 
    /// with an external login using the specified <paramref name="primaryId"/> and <paramref name="type"/>.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <param name="primaryId">The unique identifier from the external authorization provider.</param>
    /// <param name="type">The external authorization provider type.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The asynchronous operation result containing the <see cref="IdentityResult"/>.</returns>
    Task<IdentityResult> CreateAsyncByExternalIdAsync(BaseUser user, string primaryId, AuthType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new external login to an existing <paramref name="user"/> 
    /// using the specified <paramref name="primaryId"/> and <paramref name="type"/>.
    /// </summary>
    /// <param name="user">The existing user.</param>
    /// <param name="primaryId">The unique identifier from the external authorization provider.</param>
    /// <param name="type">The external authorization provider type.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The asynchronous operation result containing the <see cref="IdentityResult"/>.</returns>
    Task<IdentityResult> AddOauthExternalIdAsync(BaseUser user, string primaryId, AuthType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user associated with the specified external login credentials.
    /// </summary>
    /// <param name="primaryId">The unique identifier from the external authorization provider.</param>
    /// <param name="type">The external authorization provider type.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The user entity if found; otherwise, <c>null</c>.</returns>
    Task<BaseUser?> FindUserByPrimaryIdAsync(string primaryId, AuthType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user by their internal user ID.
    /// </summary>
    /// <param name="userId">The internal user ID (as a string).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The user entity if found; otherwise, <c>null</c>.</returns>
    new Task<BaseUser?> FindByIdAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all OAuth authentications associated with a given user.
    /// </summary>
    /// <param name="userId">The internal user ID.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of <see cref="OAuthAuthentication"/> entities.</returns>
    Task<List<OAuthAuthentication>> GetUserOAuthAuthentication(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new "Notify Me" request to the store.
    /// </summary>
    /// <param name="notifyMe">The "Notify Me" request entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task AddNotifyMe(NotifyMe notifyMe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new feedback entry to the store.
    /// </summary>
    /// <param name="feedback">The feedback entity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task AddFeedback(Feedback feedback, CancellationToken cancellationToken = default);
}