namespace PlaylistService.Models.Entities;

/// <inheritdoc />
public class RecordLabelMetadata : CatalogMetadata
{
    /// <summary>
    /// Default .ctor
    /// </summary>
    public RecordLabelMetadata()
    {
        Type = "record";
    }
}