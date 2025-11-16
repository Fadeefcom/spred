namespace TrackService.Models.Enums;

/// <summary>
/// Specifies the type of filter to be applied.
/// </summary>
public enum FilterType
{
    /// <summary>
    /// No filter applied.
    /// </summary>
    None,

    /// <summary>
    /// Filter items that contain a specified value.
    /// </summary>
    Contains,

    /// <summary>
    /// Filter items that are equal to a specified value.
    /// </summary>
    Equal,

    /// <summary>
    /// Filter items that are not equal to a specified value.
    /// </summary>
    NotEqual
}
