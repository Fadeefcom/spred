namespace InferenceService.Helpers;

/// <summary>
/// Provides utility methods related to catalog type normalization.
/// </summary>
public static class CatalogTypeHelper
{
    /// Normalizes the catalog type string by standardizing specific values to more generic terms
    /// and returning an empty string for null or whitespace inputs.
    /// <param name="catalogType">The catalog type string to be normalized. Can be null or whitespace.</param>
    /// <returns>
    /// A normalized string representing the catalog type. Returns "playlist" for "PlaylistMetadata"
    /// (case-insensitive), "record_label" for "RecordLabelMetadata" (case-insensitive), or the original
    /// string if it does not match these specific values. Returns an empty string if the input is null or whitespace.
    /// </returns>
    public static string NormalizeCatalogType(string? catalogType)
    {
        if (string.IsNullOrWhiteSpace(catalogType))
            return string.Empty;

        if (string.Equals(catalogType, "PlaylistMetadata", StringComparison.InvariantCultureIgnoreCase))
            return "playlist";

        if (string.Equals(catalogType, "RecordLabelMetadata", StringComparison.InvariantCultureIgnoreCase))
            return "record_label";

        return catalogType;
    }

    /// Determines whether the catalog type string is valid based on specific normalized values.
    /// <param name="catalogType">The catalog type string to validate. Can be null or whitespace.</param>
    /// <returns>
    /// A boolean value indicating whether the catalog type is valid. Returns true if the input matches
    /// "playlist" or "record_label" (case-insensitive), otherwise returns false.
    /// </returns>
    public static bool IsValid(string? catalogType)
    {
        return string.Equals(catalogType, "playlist", StringComparison.InvariantCultureIgnoreCase)
               || string.Equals(catalogType, "record_label", StringComparison.InvariantCultureIgnoreCase);
    }
}