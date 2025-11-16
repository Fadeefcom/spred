namespace AggregatorService.Models.Dto;

/// <summary>
/// Represents a radio station with metadata details such as name, location, reach, and timezone.
/// </summary>
public record RadioInfo
{
    /// <summary>
    /// Gets or sets the unique identifier in a URL-friendly string format for the radio station.
    /// It is commonly used to reference the radio station in various operations or as a key
    /// when caching data for performance optimization.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the radio station. This property represents the
    /// title or designation used to identify the station and is typically displayed
    /// in user-facing interfaces or metadata about the station.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the city where the radio station is located.
    /// </summary>
    public string CityName { get; set; } = string.Empty;

    /// <summary>
    /// Represents the country code corresponding to the radio station's location.
    /// </summary>
    /// <remarks>
    /// The country code is typically represented as a two-letter ISO 3166-1 alpha-2 format.
    /// For example, "US" for the United States or "GB" for Great Britain.
    /// This property is useful for identifying the origin of the radio station.
    /// </remarks>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the country where the radio station is based.
    /// </summary>
    public string CountryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time zone information associated with the radio station, represented as a string.
    /// </summary>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated audience size or coverage of the radio station.
    /// This value represents the radio station's reach, typically measured by the
    /// number of listeners or the geographical area it covers.
    /// </summary>
    public int Reach { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the radio station was first aired.
    /// </summary>
    /// <remarks>
    /// This property represents the original broadcasting date and time of the radio station,
    /// if available. The value is expressed as a nullable DateTimeOffset to handle cases
    /// where this information may not be provided.
    /// </remarks>
    public DateTimeOffset? FirstAiredAt { get; set; }

    /// Gets or sets the URL of the image associated with the radio.
    /// Typically used to store a link to the logo or promotional image
    /// relevant to the radio station.
    public string ImageUrl { get; set; } = string.Empty;
}