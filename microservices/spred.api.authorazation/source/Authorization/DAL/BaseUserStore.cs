using Authorization.Abstractions;
using Authorization.Models.Entities;
using Authorization.Options;
using Extensions.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Interfaces;

namespace Authorization.DAL;

/// <summary>
/// Custom user store implementation for <see cref="BaseUser"/>, 
/// backed by <see cref="IPersistenceStore{T,TKey}"/> for Cosmos DB.
/// </summary>
public class BaseUserStore : IUserPlusStore
{
    private readonly IPersistenceStore<BaseUser, Guid> _users;
    private readonly IPersistenceStore<OAuthAuthentication, Guid> _oauth;
    private readonly IPersistenceStore<NotifyMe, Guid> _notify;
    private readonly IPersistenceStore<Feedback, Guid> _feedback;
    private readonly IPersistenceStore<UserToken, Guid> _userToken;
    private readonly IPersistenceStore<BaseRole, Guid> _roles;
    private readonly ILogger<IUserPlusStore> _logger;
    private readonly ILookupNormalizer _lookupNormalizer;

    /// <summary>
    /// Creates a new instance of <see cref="BaseUserStore"/>.
    /// </summary>
    public BaseUserStore(
        IPersistenceStore<BaseUser, Guid> users,
        IPersistenceStore<OAuthAuthentication, Guid> oauth,
        IPersistenceStore<NotifyMe, Guid> notify,
        IPersistenceStore<Feedback, Guid> feedback,
        IPersistenceStore<UserToken, Guid> userToken,
        IPersistenceStore<BaseRole, Guid> roles,
        ILogger<IUserPlusStore> logger, ILookupNormalizer normalizer)
    {
        _users = users;
        _oauth = oauth;
        _notify = notify;
        _feedback = feedback;
        _logger = logger;
        _lookupNormalizer = normalizer;
        _userToken = userToken;
        _roles = roles;
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public Task<string> GetUserIdAsync(BaseUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Id.ToString());

    /// <inheritdoc cref="IUserPlusStore" />
    public Task<string?> GetUserNameAsync(BaseUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.UserName);

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task SetUserNameAsync(BaseUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName ?? string.Empty;
        await SetNormalizedUserNameAsync(user, user.UserName, cancellationToken);
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public Task<string?> GetNormalizedUserNameAsync(BaseUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedUserName);

    /// <inheritdoc cref="IUserPlusStore" />
    public Task SetNormalizedUserNameAsync(BaseUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = _lookupNormalizer.NormalizeName(normalizedName);
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task<IdentityResult> CreateAsync(BaseUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(user.UserName))
            return IdentityResult.Failed(new IdentityError { Code = nameof(IdentityErrorDescriber.InvalidUserName) });

        user.Id = user.Id == Guid.Empty ? Guid.NewGuid() : user.Id;
        await SetNormalizedUserNameAsync(user, user.UserName, cancellationToken);
        await SetNormalizedEmailAsync(user, user.Email, cancellationToken);
        await SetSecurityStampAsync(user, Guid.NewGuid().ToString("N"), cancellationToken);

        var res = await _users.StoreAsync(user, cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess)
        {
            LogSpredErrors("CreateAsync", $"User create failed, for {user.Id}", res.Exceptions);
            return IdentityResult.Failed(new IdentityError
                { Code = "StoreFailed", Description = "Store User async failed" });
        }

        _logger.LogSpredInformation("Create user", $"User created {user.Id}");
        return IdentityResult.Success;
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task<IdentityResult> UpdateAsync(BaseUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var expected = user.ETag;
        if (string.IsNullOrEmpty(expected))
            return IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure" });

        var res = await _users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess)
        {
            LogSpredErrors("UpdateAsync", $"User update failed, for {user.Id}", res.Exceptions);
            return IdentityResult.Failed(new IdentityError
            {
                Code = "",
                Description = res.Exceptions.FirstOrDefault()?.Message
                              ?? $"Update user async failed with userID: {user.Id}"
            });
        }

        _logger.LogSpredInformation("Update user async", "User updated {user.Id}");
        return IdentityResult.Success;
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task<IdentityResult> DeleteAsync(BaseUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _users.DeleteAsync(user.Id, new PartitionKey(user.Id.ToString()), cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            LogSpredErrors("DeleteAsync", $"User delete failed, for {user.Id}", result.Exceptions);
            return IdentityResult.Failed(new IdentityError { Code = "DeleteFailed" });
        }

        _logger.LogSpredInformation("Delete user async", $"User delete requested {user.Id}");
        return IdentityResult.Success;
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task<BaseUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Guid.TryParse(userId, out var id))
            return null;

        var res = await _users.GetAsync(id, new PartitionKey(userId), cancellationToken, noCache: true).ConfigureAwait(false);
        return res.Result;
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public Task<BaseUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
        throw new NotImplementedException("FindByNameAsync");

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task SetEmailAsync(BaseUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        await SetNormalizedEmailAsync(user, email, cancellationToken);
        if (!string.IsNullOrWhiteSpace(email))
            await SetSecurityStampAsync(user, Guid.NewGuid().ToString("N"), cancellationToken);
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public Task<string?> GetEmailAsync(BaseUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.Email);

    /// <inheritdoc cref="IUserPlusStore" />
    public Task<bool> GetEmailConfirmedAsync(BaseUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.EmailConfirmed);

    /// <inheritdoc cref="IUserPlusStore" />
    public Task SetEmailConfirmedAsync(BaseUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public Task<BaseUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) 
        => throw new NotImplementedException("FindByEmailAsync");

    /// <inheritdoc cref="IUserPlusStore" />
    public Task<string?> GetNormalizedEmailAsync(BaseUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.NormalizedEmail);

    /// <inheritdoc cref="IUserPlusStore" />
    public Task SetNormalizedEmailAsync(BaseUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = _lookupNormalizer.NormalizeEmail(normalizedEmail);
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task SetPasswordHashAsync(BaseUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        await SetSecurityStampAsync(user, Guid.NewGuid().ToString("N"), cancellationToken);
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public Task<string?> GetPasswordHashAsync(BaseUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.PasswordHash);

    /// <inheritdoc cref="IUserPlusStore" />
    public Task<bool> HasPasswordAsync(BaseUser user, CancellationToken cancellationToken) =>
        Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    /// <inheritdoc cref="IUserPlusStore" />
    public Task SetSecurityStampAsync(BaseUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public Task<string?> GetSecurityStampAsync(BaseUser user, CancellationToken cancellationToken) =>
        Task.FromResult(user.SecurityStamp);

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task<IdentityResult> CreateAsyncByExternalIdAsync(BaseUser user, string primaryId, AuthType type, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await FindUserByPrimaryIdAsync(primaryId, type, cancellationToken).ConfigureAwait(false);
        if (existing != null)
            return IdentityResult.Success;
        
        var create = await CreateAsync(user, cancellationToken).ConfigureAwait(false);
        if (!create.Succeeded)
            return create;
            
        var link = new OAuthAuthentication { SpredUserId = user.Id, PrimaryId = primaryId, OAuthProvider = type.ToString() };
        var res = await _oauth.StoreAsync(link, cancellationToken).ConfigureAwait(false);

        if (!res.IsSuccess)
        {
            LogSpredErrors("CreateAsyncByExternalIdAsync", $"OAuthAuthentication failed, for {user.Id}", res.Exceptions);
            return IdentityResult.Failed(new IdentityError
                { Code = "StoreFailed", Description = "Store OAuthAuthentication async failed" });
        }

        _logger.LogSpredInformation("CreateAsyncByExternalIdAsync", $"OAuth link created {user.Id}");
        return IdentityResult.Success;
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task<IdentityResult> AddOauthExternalIdAsync(BaseUser user, string primaryId, AuthType type, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await FindUserByPrimaryIdAsync(primaryId, type, cancellationToken).ConfigureAwait(false);

        if (existing != null)
            return IdentityResult.Success;

        var link = new OAuthAuthentication { SpredUserId = user.Id, PrimaryId = primaryId, OAuthProvider = type.ToString() };
        var res = await _oauth.StoreAsync(link, cancellationToken).ConfigureAwait(false);

        if (!res.IsSuccess)
        {
            LogSpredErrors("AddOauthExternalIdAsync", $"OAuthAuthentication failed, for {user.Id}", res.Exceptions);
            return IdentityResult.Failed(new IdentityError
                { Code = "StoreFailed", Description = "Store OAuthAuthentication async failed." });
        }

        _logger.LogSpredInformation("AddOauthExternalIdAsync", $"OAuth link added {user.Id}");
        return IdentityResult.Success;
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task<BaseUser?> FindUserByPrimaryIdAsync(string primaryId, AuthType type, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var partitionKey = new PartitionKeyBuilder().Add(primaryId).Add(type.ToString()).Build();
        var links = await _oauth.GetAsync(x => true, x => x.Timestamp, partitionKey, 0, 1, false, cancellationToken, true)
                                .ConfigureAwait(false);

        var link = links.Result?.FirstOrDefault();
        if (link == null)
            return null;

        var res = await _users.GetAsync(link.SpredUserId, new PartitionKey(link.SpredUserId.ToString()), cancellationToken, true)
                              .ConfigureAwait(false);
        return res.Result;
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public Task<List<OAuthAuthentication>> GetUserOAuthAuthentication(Guid userId, CancellationToken cancellationToken = default) 
        => throw new NotImplementedException("GetUserOAuthAuthentication");

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task AddNotifyMe(NotifyMe notifyMe, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        notifyMe.NormalizedEmail = _lookupNormalizer.NormalizeEmail(notifyMe.NormalizedEmail);
        var res = await _notify.StoreAsync(notifyMe, cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess)
            LogSpredErrors("AddNotifyMe", $"Store NotifyMe failed, for {notifyMe.Id}", res.Exceptions);
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task AddFeedback(Feedback feedback, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var res = await _feedback.StoreAsync(feedback, cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess)
            LogSpredErrors("AddFeedback", $"Store Feedback failed, for {feedback.Id}", res.Exceptions);
    }
    
    /// <inheritdoc />
    public async Task AddToRoleAsync(BaseUser user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var norm = _lookupNormalizer.NormalizeName(roleName);
        var res = 
            await _roles.GetAsync(x => true, x => x.Timestamp, 
                partitionKey: new PartitionKey(norm), offset: 0, limit: 1, descending: false, 
                cancellationToken: cancellationToken, noCache: true).ConfigureAwait(false);

        if (!res.IsSuccess)
        {
            LogSpredErrors("AddToRoleAsync", $"Get role by name failed, for {roleName}", res.Exceptions);
            throw res.Exceptions.First();
        }

        var role = res.Result?.FirstOrDefault();
        
        if (role == null)
            return;
        
        user.UserRoles.Add(role.NormalizedName!);
        await UpdateAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveFromRoleAsync(BaseUser user, string roleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (user.UserRoles.Remove(roleName))
        {
            var res = await _users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            if (!res.IsSuccess)
            {
                LogSpredErrors("RemoveFromRoleAsync", $"Remove from role failed, for {user.Id}", res.Exceptions);
                throw res.Exceptions.First();
            }
        }
        
        _logger.LogSpredInformation("Remove user from role", $"User {user.Id} -/-> Role {roleName}");
    }

    /// <inheritdoc />
    public Task<IList<string>> GetRolesAsync(BaseUser user, CancellationToken cancellationToken)
        => Task.FromResult<IList<string>>(user.UserRoles.ToList());

    /// <inheritdoc />
    public Task<bool> IsInRoleAsync(BaseUser user, string roleName, CancellationToken cancellationToken)
        => Task.FromResult(user.UserRoles.Any(r => r == roleName));

    /// <inheritdoc />
    public Task<IList<BaseUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        => throw new NotImplementedException(nameof(GetUsersInRoleAsync));

    /// <inheritdoc cref="IUserPlusStore" />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task SetTokenAsync(BaseUser user, string loginProvider, string name, string? value,
        CancellationToken cancellationToken)
    {
        var token = new UserToken()
        {
            LoginProvider = loginProvider,
            Name = name,
            Value = value,
            UserId = user.Id
        };
        
        var res = await _userToken.StoreAsync(token, cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess)
            LogSpredErrors("SetTokenAsync", $"Store UserToken failed, for {user.Id} {loginProvider}:{name}", res.Exceptions);
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task RemoveTokenAsync(BaseUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        var token = await _userToken.GetAsync(x => x.Name == name, x => x.Timestamp, new PartitionKeyBuilder().Add(user.Id.ToString()).Add(loginProvider).Build(), 
            0, 1, false, cancellationToken, true).ConfigureAwait(false);
        
        if (token.IsSuccess && !string.IsNullOrWhiteSpace(token.Result?.FirstOrDefault()?.Value))
        {
            var del = await _userToken.DeleteAsync(token.Result.First(), cancellationToken).ConfigureAwait(false);
            if (!del.IsSuccess)
                LogSpredErrors("RemoveTokenAsync", $"Delete UserToken failed, for {user.Id} {loginProvider}:{name}", del.Exceptions);
        }
    }

    /// <inheritdoc cref="IUserPlusStore" />
    public async Task<string?> GetTokenAsync(BaseUser user, string loginProvider, string name, CancellationToken cancellationToken)
    {
        var result = await _userToken.GetAsync(x => x.Name == name, x => x.Timestamp, new PartitionKeyBuilder().Add(user.Id.ToString()).Add(loginProvider).Build(), 
            0, 1, false, cancellationToken, true).ConfigureAwait(false);
        
        if (!result.IsSuccess)
            LogSpredErrors("GetTokenAsync", $"Get UserToken failed, for {user.Id} {loginProvider}:{name}", result.Exceptions);

        return result.Result?.FirstOrDefault()?.Value;
    }
    
    private void LogSpredErrors(string action, string message, IEnumerable<System.Exception> exceptions)
    {
        var enumerable = exceptions as System.Exception[] ?? exceptions.ToArray();
        
        if (enumerable.Length == 0)
            return;

        foreach (var ex in enumerable)
        {
            _logger.LogSpredError(action, message, ex);
        }
    }
}