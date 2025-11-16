using System.Security.Claims;
using Authorization.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace Authorization.Configuration;

/// <summary>
/// Role seed
/// </summary>
public static class RoleSeed
{
    private static readonly Dictionary<string, (string Type, string Value)[]> _roleClaims = new()
    {
        ["Artist"] =
        [
            ("permission:track:*","own"),
            ("permission:playlist:read","own")
        ],
        ["Curator"] =
        [
            ("permission:track:*","own"),
            ("permission:playlist:*","own"),
            ("permission:analytics:view","own")
        ]
    };
    
    /// <summary>
    /// Default roles
    /// </summary>
    public static readonly HashSet<string> AllowedDefaults = new(StringComparer.OrdinalIgnoreCase)
    {
        "Artist"
    };

    /// <summary>
    /// Initializes roles in the system if they do not already exist.
    /// </summary>
    /// <param name="scope">The service scope to resolve dependencies.</param>
    public static async Task InitRoles(this IServiceScope scope)
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<BaseRole>>();

        foreach (var (roleName, claims) in _roleClaims)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new BaseRole(roleName);
                foreach (var claim in claims)
                    role.RoleClaims.Add(claim.Type, claim.Value);
                
                await roleManager.CreateAsync(role);
            }
            else
            {
                var existing = await roleManager.GetClaimsAsync(role);
                var missing = claims.Where(c => !existing.Any(e => e.Type == c.Type && e.Value == c.Value)).ToArray();
                foreach (var (type, value) in missing)
                {
                    var res = await roleManager.AddClaimAsync(role!, new Claim(type, value));
                    if (!res.Succeeded) continue;
                    role = await roleManager.FindByNameAsync(roleName);
                }
            }
        }
    }
}