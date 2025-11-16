using System.ComponentModel.DataAnnotations;

namespace Authorization.Models.Dto;

/// <summary>
/// Feedback form 
/// </summary>
public sealed record FeedbackForm
{
    /// <summary>
    /// Represents the user's feedback input in text form.
    /// </summary>
    /// <remarks>
    /// The feedback property is expected to capture text input from the user,
    /// providing details about their thoughts, opinions, or comments.
    /// </remarks>
    [StringLength(100, MinimumLength = 1)]
    public required string Subject { get; init; }

    /// <summary>
    /// Represents the user's feedback message.
    /// </summary>
    [StringLength(500, MinimumLength = 1)]
    public required string Message { get; init; }

    /// <summary>
    /// Represents the user's email address.
    /// </summary>
    [StringLength(50, MinimumLength = 1)]
    public required string FeedbackType { get; init; } 
}