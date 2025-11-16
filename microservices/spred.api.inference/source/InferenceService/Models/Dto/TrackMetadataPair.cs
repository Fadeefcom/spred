namespace InferenceService.Models.Dto;

/// <summary>
/// Represents a pairing between a metadata item and a user.
/// </summary>
public sealed record TrackMetadataPair
{
    /// <summary>
    /// The unique identifier of the metadata associated with a track.
    /// </summary>
    public Guid MetadataId { get; set; }

    /// <summary>
    /// The unique identifier of the user linked to the metadata.
    /// </summary>
    public Guid MetadataOwner { get; set; }
}