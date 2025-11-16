using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Configurations;

/// <summary>
/// Configuration options for accessing the Chartmetrics API.
/// Contains credentials or tokens needed for authentication.
/// </summary>
public record ChartmetricOptions
{
    /// <summary>
    /// Section name.
    /// </summary>
    public const string SectionName = "Chartmetric";
    
    /// <summary>
    /// The refresh token used to obtain access tokens from the Chartmetrics API.
    /// </summary>
    [StringLength(100, MinimumLength = 1)]
    public required string RefreshToken { get; init; }
}