using Authorization.Models.Entities;

namespace Authorization.Models.Dto;

/// <summary>
/// User dto
/// </summary>
public class UserDto
{
    /// <summary>
    /// ID
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// UserName
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// User roles
    /// </summary>
    public string[] Roles { get; set; } = [];
    
    /// <summary>
    /// Email
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// User location
    /// </summary>
    public string Location { get; set; } = string.Empty;
    
    /// <summary>
    /// User bio
    /// </summary>
    public string Bio { get; set; } = string.Empty;
    
    /// <summary>
    /// User avatar
    /// </summary>
    public string AvatarUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Flag is user just registered
    /// used in analytics
    /// </summary>
    public bool JustRegistered { get; set; }
}

/// <summary>
/// User Extension
/// </summary>
public static class UserExtension
{
    /// <summary>
    /// If user just registred
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public static bool JustRegistered(BaseUser user)
        => user.Created.AddHours(24) >= DateTime.UtcNow;

}