using System.Security.Claims;
using Authorization.Models.Entities;
using Extensions.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Interfaces;

namespace Authorization.DAL;

/// <summary>
/// Role store implementation
/// </summary>
public class RoleStore : IRoleClaimStore<BaseRole>
{
    private readonly IPersistenceStore<BaseRole, Guid> _roles;
    private readonly ILogger<RoleStore> _logger;
    private readonly ILookupNormalizer _normalizer;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="roles"></param>
    /// <param name="normalizer"></param>
    /// <param name="logger"></param>
    public RoleStore(
        IPersistenceStore<BaseRole, Guid> roles,
        ILookupNormalizer normalizer,
        ILogger<RoleStore> logger)
    {
        _roles = roles;
        _logger = logger;
        _normalizer = normalizer;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public Task<string> GetRoleIdAsync(BaseRole role, CancellationToken cancellationToken) =>
        Task.FromResult(role.Id.ToString());

    /// <inheritdoc />
    public Task<string?> GetRoleNameAsync(BaseRole role, CancellationToken cancellationToken) =>
        Task.FromResult(role.Name);

    /// <inheritdoc />
    public async Task SetRoleNameAsync(BaseRole role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName ?? string.Empty;
        await SetNormalizedRoleNameAsync(role, roleName, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string?> GetNormalizedRoleNameAsync(BaseRole role, CancellationToken cancellationToken) =>
        Task.FromResult(role.NormalizedName);

    /// <inheritdoc />
    public Task SetNormalizedRoleNameAsync(BaseRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = _normalizer.NormalizeName(normalizedName);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IdentityResult> CreateAsync(BaseRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await SetNormalizedRoleNameAsync(role, role.Name, cancellationToken);

        var res = await _roles.StoreAsync(role, cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess)
        {
            LogSpredErrors("CreateAsync", $"Role create failed, for {role.Id}", res.Exceptions);
            return IdentityResult.Failed(new IdentityError { Code = "RoleStoreFailed" });
        }

        _logger.LogSpredInformation("Create role", $"Role created {role.Id} {role.NormalizedName}");
        return IdentityResult.Success;
    }

    /// <inheritdoc />
    public async Task<IdentityResult> UpdateAsync(BaseRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(role.ETag)) return IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure" });

        await SetNormalizedRoleNameAsync(role, role.Name, cancellationToken);

        var res = await _roles.UpdateAsync(role, cancellationToken).ConfigureAwait(false);
        if (!res.IsSuccess)
        {
            LogSpredErrors("UpdateAsync", $"Role create failed, for {role.Id}", res.Exceptions);
            var description = res.Exceptions.FirstOrDefault()?.Message ?? $"Update role failed {role.Id}";
            return IdentityResult.Failed(new IdentityError { Code = "RoleUpdateFailed", Description = description });
        }

        _logger.LogSpredInformation("Update role", $"Role updated {role.Id} {role.NormalizedName}");
        return IdentityResult.Success;
    }

    /// <inheritdoc />
    public async Task<IdentityResult> DeleteAsync(BaseRole role, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _roles.DeleteAsync(role, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            LogSpredErrors("DeleteAsync", $"Role delete failed, for {role.Id}", result.Exceptions);
            return IdentityResult.Failed(new IdentityError { Code = "RoleDeleteFailed" });
        }

        _logger.LogSpredInformation("Delete role", $"Role delete requested {role.Id}");
        return IdentityResult.Success;
    }

    /// <inheritdoc />
    public Task<BaseRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        => throw new NotImplementedException(nameof(FindByIdAsync));

    /// <inheritdoc />
    public async Task<BaseRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var norm = _normalizer.NormalizeName(normalizedRoleName);
        var res = 
            await _roles.GetAsync(x => true, x => x.Timestamp, 
                partitionKey: new PartitionKey(norm), offset: 0, limit: 1, descending: false, 
                cancellationToken: cancellationToken, noCache: true).ConfigureAwait(false);
        
        if(!res.IsSuccess)
            LogSpredErrors("Find role By Name Async", $"Role find failed, for {normalizedRoleName}", res.Exceptions);
        
        return res.Result?.FirstOrDefault();
    }

    /// <inheritdoc />
    public Task<IList<Claim>> GetClaimsAsync(BaseRole role, CancellationToken cancellationToken = default) 
        => Task.FromResult<IList<Claim>>(role.RoleClaims.Select(r => new Claim(r.Key, r.Value)).ToList());

    /// <inheritdoc />
    public async Task AddClaimAsync(BaseRole role, Claim claim, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (role.RoleClaims.TryAdd(claim.Type, claim.Value))
        {
            var res =await _roles.UpdateAsync(role, cancellationToken).ConfigureAwait(false);
            if (!res.IsSuccess)
            {
                LogSpredErrors("AddClaimAsync", $"Add claim to role failed, for {role.Id}", res.Exceptions);
                throw new InvalidOperationException("AddClaimFailed");
            }
        }

        _logger.LogSpredInformation("Add role claim", $"Role {role.Id} claim {claim.Type}={claim.Value}");
    }

    /// <inheritdoc />
    public async Task RemoveClaimAsync(BaseRole role, Claim claim, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if(role.RoleClaims.Remove(claim.Type))
            await _roles.UpdateAsync(role, cancellationToken: cancellationToken);
        
        _logger.LogSpredInformation("Remove role claim", $"Role {role.Id} claim {claim.Type}={claim.Value}");
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