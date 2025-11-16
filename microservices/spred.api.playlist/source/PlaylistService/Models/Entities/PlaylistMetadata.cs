using PlaylistService.Models.Commands;

namespace PlaylistService.Models.Entities;

/// <inheritdoc />
public class PlaylistMetadata : CatalogMetadata
{
    /// <summary>
    /// Default .ctor
    /// </summary>
    public PlaylistMetadata() : base()
    {
        Type = "playlist";
    }

    /// <inheritdoc />
    public override void Update(UpdateMetadataCommand metadata)
    {
        base.Update(metadata);
        Type = "playlist";
    }
}