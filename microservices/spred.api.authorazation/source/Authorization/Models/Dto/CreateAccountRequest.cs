namespace Authorization.Models.Dto;

/// <summary>
/// Represents a request to create a new account on a specific platform.
/// </summary>
public record CreateAccountRequest(string Platform, string AccountId);
