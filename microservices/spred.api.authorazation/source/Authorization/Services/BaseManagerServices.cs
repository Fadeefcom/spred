using System.Security.Claims;
using System.Text;
using Authorization.Abstractions;
using Authorization.Helpers;
using Authorization.Models.Dto;
using Authorization.Models.Entities;
using Authorization.Options;
using Authorization.Options.AuthenticationSchemes;
using AutoMapper;
using Exception;
using Extensions.Extensions;
using Extensions.Interfaces;
using Extensions.Models;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Refit;
using Spred.Bus.Contracts;
using StackExchange.Redis;

namespace Authorization.Services;

/// <summary>
/// Provides a set of services and methods for managing users, specifically extending the functionality of the UserManager class.
/// </summary>
/// <remarks>
/// This class is designed to handle user-related operations, including user creation, updating, authentication,
/// external authentication management, and feedback functionality.
/// It inherits from UserManager and enhances its capabilities to support operations tailored to the application's authorization needs.
/// </remarks>
public class BaseManagerServices : UserManager<BaseUser>
{
    private readonly IUserPlusStore _userStore;
    private readonly IAggregatorApi _aggregatorApi;
    private readonly ISpotifyApi _spotifyApi;
    private readonly IGetToken _getToken;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserManager<BaseUser>> _logger;
    private readonly IRoleClaimStore<BaseRole> _roleStore;
    private readonly ILinkedAccountEventStore _eventStore;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDatabase _redis;
    private readonly IMapper _mapper;
    
    private const string ChallengeProvider = "Spred.AccountChallenge";
    private static string BuildTokenName(string accountId, AccountPlatform platform) => $"account:{platform.ToString().ToLowerInvariant()}:{accountId}";

    /// <summary>
    /// Constructor for BaseManagerServices 
    /// </summary>
    /// <param name="store">The user store implementation for managing user data.</param>
    /// <param name="getToken">Service for generating tokens for internal and external use.</param>
    /// <param name="optionsAccessor">Accessor for identity options configuration.</param>
    /// <param name="options">Outer service options containing service URLs.</param>
    /// <param name="configuration">Application configuration settings.</param>
    /// <param name="passwordHasher">Password hasher for user password management.</param>
    /// <param name="userValidators">Collection of user validators for validation logic.</param>
    /// <param name="passwordValidators">Collection of password validators for password rules.</param>
    /// <param name="keyNormalizer">Normalizer for user keys like usernames.</param>
    /// <param name="errors">Describer for identity-related errors.</param>
    /// <param name="services">Service provider for dependency injection.</param>
    /// <param name="logger">Logger for logging operations and errors.</param>
    /// <param name="publishEndpoint"></param>
    /// <param name="redis"></param>
    /// <param name="roleStore"></param>
    /// <param name="eventStore"></param>
    /// <param name="mapper"></param>
    public BaseManagerServices(
        IUserPlusStore store,
        IGetToken getToken,
        IOptions<IdentityOptions> optionsAccessor,
        IOptions<ServicesOuterOptions> options,
        IConfiguration configuration,
        IPasswordHasher<BaseUser> passwordHasher,
        IEnumerable<IUserValidator<BaseUser>> userValidators,
        IEnumerable<IPasswordValidator<BaseUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<BaseUser>> logger,
        IPublishEndpoint publishEndpoint,
        IConnectionMultiplexer redis,
        IRoleClaimStore<BaseRole> roleStore, ILinkedAccountEventStore eventStore,
        IMapper mapper)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
        _logger =logger;
        _userStore = store;
        _aggregatorApi = RestService.For<IAggregatorApi>(options.Value.AggregatorService);
        _spotifyApi = RestService.For<ISpotifyApi>("https://accounts.spotify.com");
        _getToken = getToken;
        _configuration = configuration;
        _roleStore = roleStore;
        _eventStore = eventStore;
        _publishEndpoint = publishEndpoint;
        _redis = redis.GetDatabase();
        _mapper = mapper;
    }

    /// <summary>
    /// Finds a user by their primary ID and authentication type.
    /// </summary>
    /// <param name="primaryId">The primary ID of the user.</param>
    /// <param name="type">The authentication type (e.g., Base, Spotify).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The user if found, otherwise null.</returns>
    public async Task<BaseUser?> FindByPrimaryId(string primaryId, AuthType type,
        CancellationToken cancellationToken = default)
    {
        return type != AuthType.Base
            ? await _userStore.FindUserByPrimaryIdAsync(primaryId, type, cancellationToken)
            : null;
    }

    /// <inheritdoc />
    public override Task<BaseUser?> FindByIdAsync(string userId)
    {
        return _userStore.FindByIdAsync(userId, CancellationToken.None);
    }

    /// <summary>
    /// Creates a user using an external ID if not already present.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <param name="primaryId">The external primary ID.</param>
    /// <param name="type">The authentication type.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The result of the creation operation.</returns>
    public async Task<IdentityResult> CreateAsyncByExternalIdAsync(BaseUser user, string primaryId, AuthType type,
        CancellationToken cancellationToken = default)
    {
        var validate = await ValidateUserAsync(user);
        if (!validate.Succeeded)
            return validate;

        var result = await _userStore.CreateAsyncByExternalIdAsync(user, primaryId, type, cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                _logger.LogSpredError("CreateAsyncByExternalIdAsync", $"User creation failed. Code: {error.Code}, Description: {error.Description}",  
                    new InvalidOperationException($"Identity error: {error.Code} - {error.Description}"));
            }
        }

        if (result.Succeeded && type == AuthType.Spotify)
        {
            var token = await _getToken.GetInternalTokenAsync([]);
            var response = await _aggregatorApi.QueueUserPlaylists("Bearer " + token, new QueueUserPlaylistsRequest
            {
                ClientId = primaryId,
                SpredUserId = user.Id,
                SubmitUrls = new Dictionary<string, string>()
                {
                    { "SpredUserId", user.Id.ToString() }
                }
            });
            
            if (!response.IsSuccessStatusCode)
                _logger.LogSpredWarning("QueueUserPlaylist",$"Aggregator responded with {response.StatusCode}");
        }

        return result;
    }

    /// <summary>
    /// Adds an OAuth external ID to an existing user.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <param name="primaryId">The external primary ID.</param>
    /// <param name="type">The authentication type.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The result of the update operation.</returns>
    public async Task<IdentityResult> AddOauthExternalIdAsync(BaseUser user, string primaryId, AuthType type,
        CancellationToken cancellationToken = default)
    {
        if ((await FindByPrimaryId(primaryId, type, cancellationToken)) != null)
        {
            return IdentityResult.Failed(new IdentityError()
            {
                Code = nameof(AddOauthExternalIdAsync),
                Description = $"PrimaryId:{primaryId} with type:{type} already exists"
            });
        }

        var result = await _userStore.AddOauthExternalIdAsync(user, primaryId, type, cancellationToken);

        if (result.Succeeded && type == AuthType.Spotify)
        {
            var token = await _getToken.GetInternalTokenAsync([]);
            var response = await _aggregatorApi.QueueUserPlaylists("Bearer " + token, new QueueUserPlaylistsRequest
            {
                ClientId = primaryId,
                SpredUserId = user.Id,
                SubmitUrls = new Dictionary<string, string>()
                {
                    { "SpredUserId", user.Id.ToString() }
                }
            });
            
            if (!response.IsSuccessStatusCode)
                _logger.LogSpredWarning("QueueUserPlaylist",$"Aggregator responded with {response.StatusCode}");
        }

        return result;
    }

    /// <summary>
    /// Updates a user after validation and normalization.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <returns>The result of the update operation.</returns>
    protected override async Task<IdentityResult> UpdateUserAsync(BaseUser user)
    {
        var result = await ValidateUserAsync(user).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return result;
        }

        await UpdateNormalizedUserNameAsync(user).ConfigureAwait(false);
        await UpdateNormalizedEmailAsync(user).ConfigureAwait(false);
        return await Store.UpdateAsync(user, CancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves OAuth authentication records for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of OAuth authentication records.</returns>
    public Task<List<OAuthAuthentication>> GetUserOAuthAuthentication(Guid userId, CancellationToken cancellationToken)
    {
        return _userStore.GetUserOAuthAuthentication(userId, cancellationToken);
    }

    /// <summary>
    /// Adds a notify me request for anonymous users.
    /// </summary>
    /// <param name="notifyMe">The notify me request details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task AddAnonymousNotifyMe(NotifyMe notifyMe, CancellationToken cancellationToken)
        => _userStore.AddNotifyMe(notifyMe, cancellationToken);

    /// <summary>
    /// Adds feedback provided by a user.
    /// </summary>
    /// <param name="feedback">The feedback details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task AddFeedback(Feedback feedback, CancellationToken cancellationToken)
        => _userStore.AddFeedback(feedback, cancellationToken);

    /// <summary>
    /// Retrieves the user ID from the ClaimsPrincipal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user ID as a string.</returns>
    public override string GetUserId(ClaimsPrincipal? principal)
     => principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    /// <summary>
    /// Find user by Id.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found, otherwise null.</returns>
    public async Task<BaseUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        => await _userStore.FindByIdAsync(userId, cancellationToken);

    /// <summary>
    /// Updates the access token for a user using a refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="primaryId">The primary ID of the user.</param>
    /// <param name="authType">The authentication type.</param>
    /// <returns>The updated access token as a string.</returns>
    public async Task<string> UpdateAccessToken(string refreshToken, string primaryId, AuthType authType)
    {
        if (authType == AuthType.Spotify)
        {

            if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(primaryId))
                return string.Empty;

            var request = new TokenRequest()
            {
                GrantType = "refresh_token",
                RefreshToken = refreshToken,
                ClientId = primaryId
            };

            var section = _configuration.GetSection("OAuthOption").GetSection(SpotifyAuthenticationDefaults.AuthenticationScheme);

            var clientId = section["ClientId"]!;
            var clientSecret = section["ClientSecret"]!;
            var credentials = $"{clientId}:{clientSecret}";
            var encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            var authorizationHeader = $"Basic {encodedCredentials}";

            var response = await _spotifyApi.GetAccessTokenAsync(authorizationHeader, request);
            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = response.Content?.AccessToken;
                return tokenResponse ?? string.Empty;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Update user model
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="userModel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task UpdateUserByModel(string userId, UpdateUserModel userModel, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId, cancellationToken);
        if (user == null)
            throw new InvalidOperationException($"User with id {userId} not found");

        var updated = false;
        
        if (!string.IsNullOrWhiteSpace(userModel.Name) && user.UserName != userModel.Name)
        {
            user.UserName = userModel.Name;
            updated = true;
        }
        
        if (!string.IsNullOrWhiteSpace(userModel.Bio) && user.Bio != userModel.Bio)
        {
            user.Bio = userModel.Bio;
            updated = true;
        }
        
        if (!string.IsNullOrWhiteSpace(userModel.Location) && user.Location != userModel.Location)
        {
            user.Location = userModel.Location;
            updated = true;
        }

        if (updated)
        {
            await UpdateUserAsync(user).ConfigureAwait(false);
        }
    }
    
    /// <inheritdoc/>
    public override Task<BaseUser?> GetUserAsync(ClaimsPrincipal principal)
    {
        principal.ThrowBaseExceptionIfNull("Claims principal can't be null");
        var id = GetUserId(principal);
        return FindByIdAsync(id, CancellationToken.None);
    }

    /// <inheritdoc/>
    public override async Task<IdentityResult> AddToRoleAsync(BaseUser user, string role)
    {
        ThrowIfDisposed();

        var normalizedRole = NormalizeName(role);
        if (await _userStore.IsInRoleAsync(user, normalizedRole, CancellationToken).ConfigureAwait(false))
        {
            return UserAlreadyInRoleError(role);
        }
        
        var baseRole = await _roleStore.FindByNameAsync(role, CancellationToken).ConfigureAwait(false);
        if (baseRole != null)
        {
            if(user.UserRoles.Add(baseRole.NormalizedName!))
                return await _userStore.UpdateAsync(user, CancellationToken).ConfigureAwait(false);
        }

        return RoleNotFound(role);
    }
    
    /// <inheritdoc/>
    public override async Task<IdentityResult> RemoveFromRoleAsync(BaseUser user, string role)
    {
        ThrowIfDisposed();

        var normalizedRole = NormalizeName(role);
        if (!await _userStore.IsInRoleAsync(user, normalizedRole, CancellationToken).ConfigureAwait(false))
        {
            return UserNotInRoleError(role);
        }

        var baseRole = await _roleStore.FindByNameAsync(role, CancellationToken).ConfigureAwait(false);
        if (baseRole != null && user.UserRoles.Remove(baseRole.NormalizedName!))
        {
            return await _userStore.UpdateAsync(user, CancellationToken).ConfigureAwait(false);
        }

        return RoleNotFound(role);
    }

    /// <summary>
    /// Retrieves a collection of user accounts associated with a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose accounts are being retrieved.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A collection of tuples containing the platform and account ID for each user account.</returns>
    public async Task<List<UserAccountDto>> GetUserAccountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await FindByIdAsync(userId.ToString(), cancellationToken);
        if (user == null)
            return [];
        
        List<UserAccountDto> result = new List<UserAccountDto>();

        foreach (var account in  user.UserAccounts)
        {
            var state = await _eventStore.GetCurrentState(account.AccountId, account.Platform, userId, cancellationToken);
            var accountDto = _mapper.Map<UserAccountDto>(state);
            accountDto.ProfileUrl = account.ProfileUrl;
            result.Add(accountDto);
        }

        return result;
    }

    /// <summary>
    /// Adds a new account to the specified user if it does not already exist on the platform.
    /// </summary>
    /// <param name="userId">Unique identifier of the user to whom the account should be added.</param>
    /// <param name="request">Request object containing platform and account ID details for the new account.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A tuple containing three values:
    /// - A boolean indicating whether the account was successfully created.
    /// - The account ID of the newly created account if successful, otherwise null.
    /// - A message describing the result of the operation.
    /// </returns>
    public async Task<(bool isCreated, string? accountId, string message)> AddAccountAsync(
        Guid userId,
        CreateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!AccountPlatformHelper.PlatformMap.TryGetValue(request.Platform, out AccountPlatform platform))
            return (false, null, "invalid-platform");
        
        if (platform != AccountPlatform.Spotify)
            return (false, null, $"Platform '{platform}' does not support challenge verification. Only Spotify is supported.");

        var user = await FindByIdAsync(userId.ToString(), cancellationToken);
        if (user is null)
            return (false, null, "user-not-found");

        if (user.UserAccounts.Any(a => a.Platform == platform && a.AccountId == request.AccountId))
            return (false, null, "platform-already-linked");

        var appendResult = await _eventStore.AppendAsync(
            request.AccountId,
            userId,
            platform,
            LinkedAccountEventType.AccountCreated,
            null,
            cancellationToken);

        if (!appendResult.Succeeded)
            return (false, null, "store-failed");

        user.UserAccounts.Add(new UserAccountRef(platform, request.AccountId, $"https://open.spotify.com/user/{request.AccountId}"));
        var updateResult = await UpdateUserAsync(user);

        if (!updateResult.Succeeded)
            return (false, null, "user-update-failed");

        return (true, request.AccountId, "account-created");
    }

    /// <summary>
    /// Retrieves the account verification state for a specific account associated with a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="accountId">The unique identifier of the account.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The account verification status if found; otherwise, null.</returns>
    public async Task<(AccountStatus?, DateTimeOffset?)> GetAccountVerificationStateAsync(Guid userId, string accountId,
        CancellationToken cancellationToken = default)
    {
        var user = await FindByIdAsync(userId.ToString(), cancellationToken);
        if (user is null)
            return (null, null);
        
        var account = user.UserAccounts.FirstOrDefault(a => a.AccountId == accountId);
        if(account is null)
            return (null, null);
        
        var state = await _eventStore.GetCurrentState(account.AccountId, account.Platform, userId, cancellationToken);

        return (state?.Status, state?.CreatedAt);
    }

    /// <summary>
    /// Generates and associates a verification token with the specified user and account, using the Spred.AccountChallenge provider.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="accountId">The unique identifier of the user's account.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A tuple containing a boolean indicating the operation success and the generated token string. If the user or account is not found, returns false and an empty token string.</returns>
    public async Task<(bool isCreated, string token)> GetTokenVerification(Guid userId, string accountId, CancellationToken cancellationToken)
    {
        var user = await FindByIdAsync(userId.ToString(), cancellationToken);
        if (user is null)
            return (false, string.Empty);
        
        var account = user.UserAccounts.FirstOrDefault(a => a.AccountId == accountId);
        if(account is null)
            return (false, string.Empty);
        
        var name = BuildTokenName(accountId, account.Platform);
        var existing = await GetAuthenticationTokenAsync(user, ChallengeProvider, name);
        if (!string.IsNullOrWhiteSpace(existing))
            return (true, existing);
        
        var canIssue = await CanIssueTokenAsync(userId, accountId, account.Platform, cancellationToken);
        if (!canIssue) return (false, string.Empty);
        
        var tokenValue = $"spred-{account.Platform}-{Guid.NewGuid():N}";
        var tokenResult = await SetAuthenticationTokenAsync(user, ChallengeProvider, name, tokenValue);

        if (tokenResult.Succeeded)
        {
           await _eventStore.AppendAsync(account.AccountId, userId, account.Platform, LinkedAccountEventType.TokenIssued, null,
                cancellationToken);
            return (true, tokenValue);
        }
        
        return (false, string.Empty);
    }

    /// <summary>
    /// Initiates the process of verifying a user's linked account by generating a challenge token
    /// and publishing a verification command.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="accountId">The unique identifier of the linked account to verify.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating
    /// whether the verification process was successfully initiated.</returns>
    public async Task<(bool, string)> StartVerifyAccountAsync(Guid userId, string accountId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"account-verification:{userId}:{accountId}";
        
        try
        {
            var acquired = await _redis.StringSetAsync(cacheKey, "1", TimeSpan.FromMinutes(15), When.NotExists);
            if (!acquired)
                return (false, "Verification already in progress.");
            
            var user = await FindByIdAsync(userId.ToString(), cancellationToken);
            if (user is null)
                return (false, "User not found.");

            var account = user.UserAccounts.FirstOrDefault(a => a.AccountId == accountId);
            
            if(account is null)
               return (false, "Account not found.");

            var state = await _eventStore.GetCurrentState(account.AccountId, account.Platform, userId, cancellationToken);
            if(state is null)
                return (false, "Account not found.");
            
            var name = BuildTokenName(accountId, account.Platform);
            var token = await GetAuthenticationTokenAsync(user, ChallengeProvider, name);
            
            if(string.IsNullOrWhiteSpace(token))
                return (false, "Token not found.");

            var command = new VerifyAccountCommand(userId, accountId, account.Platform, token);
            await _publishEndpoint.Publish(command, context =>
            {
                context.MessageId = Guid.NewGuid();
                context.CorrelationId = state.CorrelationId;
                context.Headers.Set("x-spred-token-name", name);
            }, cancellationToken);

            return (true, "Verification started successfully.");
        }
        catch
        {
            await _redis.KeyDeleteAsync(cacheKey);
            throw;
        }
    }

    /// <summary>
    /// Deletes a user account by unlinking it from the user and updating the user's account records.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose account is being deleted.</param>
    /// <param name="accountId">The identifier of the account to be deleted.</param>
    /// <param name="cancellationToken">Token to observe for cancellation requests.</param>
    /// <returns>Returns true if the account was successfully deleted; otherwise, false.</returns>
    public async Task<bool> DeleteAccountAsync(Guid userId, string accountId, CancellationToken cancellationToken = default)
    {
        var user = await FindByIdAsync(userId.ToString(), cancellationToken);
        if (user is null) return false;

        var account = user.UserAccounts.FirstOrDefault(a => a.AccountId == accountId);
        if (account is null) 
            return true;
        
        if(!user.UserAccounts.Remove(account))
            return false;

        var result = await _eventStore.UnlinkAsync(account.AccountId, userId, account.Platform, cancellationToken);
        if (!result.Succeeded) return false;
        await RemoveAuthenticationTokenAsync(user, ChallengeProvider, BuildTokenName(accountId, account.Platform));
        await UpdateUserAsync(user);
        return true;
    }
    
    private async Task<bool> CanIssueTokenAsync(Guid userId, string accountId, AccountPlatform platform,  CancellationToken cancellationToken)
    {
        var state = await _eventStore.GetCurrentState(accountId, platform, userId, cancellationToken);
        if (state is null) return false;
        return state.Status is AccountStatus.Pending or AccountStatus.Error;
    }
    
    private IdentityResult UserAlreadyInRoleError(string role)
    {
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogSpredDebug("UserAlreadyInRoleError", $"User is already in role {role}.");
        }
        return IdentityResult.Failed(ErrorDescriber.UserAlreadyInRole(role));
    }

    private IdentityResult UserNotInRoleError(string role)
    {
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogSpredDebug("UserNotInRoleError", $"User is not in role {role}.");
        }
        return IdentityResult.Failed(ErrorDescriber.UserNotInRole(role));
    }
    
    private IdentityResult RoleNotFound(string role)
    {
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            Logger.LogSpredDebug("RoleNotFound", $"Role not foud {role}.");
        }
        return IdentityResult.Failed(new IdentityError()
        {
            Code = "404",
            Description = "role_not_found"
        });
    }
}
