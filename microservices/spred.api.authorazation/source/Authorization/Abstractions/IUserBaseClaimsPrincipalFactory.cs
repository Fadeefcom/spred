using System.Security.Claims;
using Authorization.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace Authorization.Abstractions;

/// <summary>
/// Defines a factory responsible for creating <see cref="ClaimsPrincipal"/> instances 
/// for <see cref="BaseUser"/> with support for specifying an authentication scheme.
/// </summary>
public interface IUserBaseClaimsPrincipalFactory : IUserClaimsPrincipalFactory<BaseUser>
{
    /// <summary>
    /// Creates a <see cref="ClaimsPrincipal"/> for the given <paramref name="user"/> 
    /// and associates it with the provided authentication <paramref name="scheme"/>.
    /// </summary>
    /// <param name="user">The user for whom to generate the claims principal.</param>
    /// <param name="scheme">
    /// The authentication scheme to associate with the generated identity 
    /// (for example, Cookie or JWT).
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. 
    /// The task result contains the generated <see cref="ClaimsPrincipal"/>.
    /// </returns>
    Task<ClaimsPrincipal> CreateAsync(BaseUser user, string scheme);
}