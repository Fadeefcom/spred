using System.ComponentModel.DataAnnotations;

namespace Authorization.Models.Dto;

/// <summary>
/// Applied form
/// </summary>
public sealed record NotifyMeFrom
{
    /// <summary>
    /// Gets or sets the username associated with the applied form.
    /// </summary>
    /// <remarks>
    /// This property represents the user's name to be used in operations such as form submissions.
    /// It is required and ensures that a non-null or non-whitespace value is provided.
    /// </remarks>
    /// [Required(ErrorMessage = "Username is required.")]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the email address associated with the applied form.
    /// </summary>
    /// <remarks>
    /// This property represents the email address provided by the user while submitting the form.
    /// It is a required field and ensures that a valid, non-null, and non-whitespace input is provided.
    /// </remarks>
    [EmailAddress]
    [StringLength(100, MinimumLength = 1)]
    public required string Email { get; init; }
    
    /// <summary>
    /// Artist type
    /// </summary>
    [RegularExpression(@"^[\p{L}\p{N}\s.,!?'-]{0,50}$", ErrorMessage = "Invalid artist type name.")]
    public string ArtistType { get; init; } = string.Empty;
    
    /// <summary>
    /// Message with in form
    /// </summary>
    [RegularExpression(@"^[\p{L}\p{N}\s.,!?'-]{0,500}$", ErrorMessage = "Invalid message.")]
    public string Message { get; init; } = string.Empty;
}