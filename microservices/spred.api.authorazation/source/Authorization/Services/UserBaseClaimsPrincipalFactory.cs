using System.Globalization;
using System.Security.Claims;
using Authorization.Abstractions;
using Authorization.Models.Entities;
using Authorization.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;

namespace Authorization.Services;

/// <summary>
/// Factory class to create <see cref="ClaimsPrincipal"/> instances for users.
/// </summary>
public class UserBaseClaimsPrincipalFactory : IUserBaseClaimsPrincipalFactory
{
    private readonly IRoleStore<BaseRole> _roleStore;
    private readonly IDatabase _redis;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserBaseClaimsPrincipalFactory"/> class.
    /// </summary>
    public UserBaseClaimsPrincipalFactory(IRoleStore<BaseRole> roleStore, IConnectionMultiplexer connectionMultiplexer) 
    {
        _roleStore = roleStore;
        _redis = connectionMultiplexer.GetDatabase();
    }

    /// <inheritdoc />
    public async Task<ClaimsPrincipal> CreateAsync(BaseUser user, string scheme)
    {
        var identity = GenerateClaimsAsync(user, scheme);
        return await CreatePrincipal(identity, user);
    }

    /// <inheritdoc />
    public async Task<ClaimsPrincipal> CreateAsync(BaseUser user)
    {
        var identity = GenerateClaimsAsync(user, CookieAuthenticationDefaults.AuthenticationScheme);
        return await CreatePrincipal(identity, user);
    }

    private async Task<ClaimsPrincipal> CreatePrincipal(ClaimsIdentity identity, BaseUser user)
    {
        var userClaimsKeys = new HashSet<string>(user.UserClaims.Keys, StringComparer.InvariantCultureIgnoreCase);

        foreach (var kv in user.UserClaims)
        {
            var claim = string.Join(' ', kv.Value);
            if (!string.IsNullOrWhiteSpace(claim))
                identity.AddClaim(new Claim(kv.Key, string.Join(' ', kv.Value)));
        }

        var roles = string.Join(' ', user.UserRoles);
        if (!string.IsNullOrWhiteSpace(roles))
            identity.AddClaim(new Claim(ClaimTypes.Role, roles));

        foreach (var kv in user.UserRoles)
        {
            var roleName = kv;

            var role = await _roleStore.FindByNameAsync(roleName, CancellationToken.None);
            if (role?.RoleClaims is null) continue;

            foreach (var rkv in role.RoleClaims)
            {
                if (userClaimsKeys.Contains(rkv.Key)) continue;
                var val = rkv.Value;
                var exists = identity.Claims.Any(c =>
                    string.Equals(c.Type, rkv.Key, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(c.Value, val, StringComparison.Ordinal));

                if (!exists)
                    identity.AddClaim(new Claim(rkv.Key, val));
            }
        }
        
        var key = $"subscription:{user.Id}";
        var raw = await _redis.StringGetAsync(key);
        var hasVal = !raw.IsNullOrEmpty;
        var ttl = await _redis.KeyTimeToLiveAsync(key);

        var isPremium = hasVal && bool.TryParse(raw.ToString(), out var b) && b && ttl.HasValue && ttl.Value > TimeSpan.Zero;
        identity.AddClaim(new Claim(ClaimTypesExtension.Premium, isPremium ? "true" : "false"));

        if (isPremium)
        {
            var exp = DateTimeOffset.UtcNow.Add(ttl!.Value).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            identity.AddClaim(new Claim(ClaimTypesExtension.PremiumExp, exp));
        }

        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Generates a <see cref="ClaimsIdentity"/> for the specified user and authentication scheme.
    /// </summary>
    /// <param name="user">The user entity.</param>
    /// <param name="scheme">The authentication scheme.</param>
    /// <returns>A <see cref="ClaimsIdentity"/> with user claims.</returns>
    private static ClaimsIdentity GenerateClaimsAsync(BaseUser user, string scheme)
    {
        var userId = user.Id;
        var userName = user.UserName;

        var id = new ClaimsIdentity(scheme, ClaimTypes.NameIdentifier, ClaimTypes.Role);
        id.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        id.AddClaim(new Claim(ClaimTypes.Name, userName!));
        id.AddClaim(new Claim(ClaimTypes.Email, user.Email ?? string.Empty));
        id.AddClaim(new Claim(ClaimTypes.Sid, user.SecurityStamp ?? string.Empty));
        id.AddClaim(new Claim(ClaimTypesExtension.Scheme, scheme));

        return id;
    }
}
