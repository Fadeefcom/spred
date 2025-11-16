namespace Authorization.Configuration;

/// <summary>
/// Provides configuration options for role name validation.
/// </summary>
public sealed class RoleValidationOptions
{
    /// <summary>
    /// Gets or sets the minimum allowed length of a role name.
    /// </summary>
    public int MinNameLength { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed length of a role name.
    /// </summary>
    public int MaxNameLength { get; set; }

    /// <summary>
    /// Gets or sets an optional regular expression pattern 
    /// that a role name must match. If null, no pattern validation is applied.
    /// </summary>
    public string? AllowedNameRegex { get; set; }
}