using System.ComponentModel.DataAnnotations;

namespace AggregatorService.Configurations;

/// <summary>
/// Represents configuration settings required for Soundcharts integration.
/// </summary>
/// <remarks>
/// This class is used to bind and validate the "Soundcharts" section in the application's configuration file.
/// The options are required to authenticate and interact with the Soundcharts API.
/// </remarks>
public class SoundchartsOptions
{
    /// <summary>
    /// Represents the configuration section name associated with Soundcharts settings.
    /// This constant is used to reference the configuration section within a configuration source.
    /// </summary>
    public const string SectionName = "Soundcharts";

    /// <summary>
    /// Gets or sets the application identifier used for authentication with the Soundcharts API.
    /// </summary>
    /// <remarks>
    /// This property is a required field and must not be an empty string. Failure to provide a valid AppId
    /// will result in an exception being thrown during application configuration or runtime initialization.
    /// </remarks>
    /// <value>
    /// Represents the unique identifier for the application, as provided by the Soundcharts API credentials.
    /// </value>
    /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
    /// Thrown when the AppId property is null or an empty string during validation.
    /// </exception>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Soundcharts:AppId is required")]
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key required for authenticating with the Soundcharts API.
    /// </summary>
    /// <remarks>
    /// This property is mandatory and cannot be empty. It is used to provide secure access to the Soundcharts API.
    /// </remarks>
    /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
    /// Thrown when the property is set to an empty string or left unset.
    /// </exception>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Soundcharts:ApiKey is required")]
    public string ApiKey { get; set; } = string.Empty;
}