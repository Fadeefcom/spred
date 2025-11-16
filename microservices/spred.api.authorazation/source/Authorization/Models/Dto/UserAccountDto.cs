namespace Authorization.Models.Dto;

/// <summary>
/// Represents a user account details data transfer object.
/// </summary>
public record UserAccountDto
{
    /// <summary>
    /// Represents the platform associated with a linked user account, such as a social media or authentication provider.
    /// </summary>
    /// <remarks>
    /// This property is used to identify the specific platform a user account is connected to.
    /// Examples might include "Google", "Facebook", or "GitHub". The value is typically derived
    /// from a mapping function during data conversion processes.
    /// </remarks>
    public string Platform { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the unique identifier for the account associated with the platform.
    /// </summary>
    public string AccountId { get; init; } = string.Empty;

    /// Gets or initializes the status of the user account.
    /// Represents the state of the linked account, usually derived
    /// from the mapped LinkedAccountState entity during data transformation.
    /// This property is expected to hold values as a string representation
    /// of the account status.
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL of the user's profile associated with the account.
    /// </summary>
    /// <remarks>
    /// This property represents a direct link to the profile on the corresponding platform.
    /// Its value is platform-specific and is expected to be a valid URL or an empty string
    /// if the profile URL is not available or applicable.
    /// </remarks>
    public string ProfileUrl { get; set; } = string.Empty;

    /// <summary>
    /// Represents the timestamp when the user's account was connected or linked.
    /// The value is expressed as a `DateTimeOffset` object, providing both the date and time
    /// including offset from UTC.
    /// </summary>
    public DateTimeOffset ConnectedAt { get; init; }

    /// <summary>
    /// Represents a user account, including platform details, account information, connection time, and profile URL.
    /// </summary>
    public UserAccountDto() { }

    /// <summary>
    /// Represents a data transfer object for a user account.
    /// This DTO encapsulates the details of a user's account
    /// on a specific platform, providing essential information such as
    /// the platform name, account ID, account status, connection timestamp,
    /// and the profile URL.
    /// </summary>
    public UserAccountDto(string platform, string accountId, string status, DateTimeOffset connectedAt, string profileUrl)
    {
        Platform = platform;
        AccountId = accountId;
        Status = status;
        ConnectedAt = connectedAt;
        ProfileUrl = profileUrl;
    }
}