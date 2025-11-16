namespace Authorization.Models.Dto;

/// <summary>
/// Update user model
/// </summary>
/// <param name="Bio"></param>
/// <param name="Location"></param>
/// <param name="Name"></param>
public record UpdateUserModel(string? Bio, string? Location, string? Name);