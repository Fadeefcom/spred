using InferenceService.Models.Entities;
using Newtonsoft.Json;

namespace InferenceService.Models.Dto;


/// <summary>
/// Represents a Data Transfer Object (DTO) for an inference playlist.
/// </summary>
public class InferenceMetadataDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the playlist.
    /// </summary>
    public Guid MetadataId { get; set; }
    
    /// <summary>
    /// Metadata owner id
    /// </summary>
    public Guid MetadataOwner { get; set; }
    
    /// <summary>
    /// Catalog type
    /// </summary>
    [JsonIgnore]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the score assigned to the playlist by the inference process.
    /// </summary>
    public string Score { get; set; }

    /// <summary>
    /// Gets or sets the rate assigned to the playlist by the inference process.
    /// </summary>
    public ReactionStatus Reaction { get; set; } = new();

    /// <summary>
    /// Track with user belongs to.
    /// </summary>
    public List<TrackUserPair> SimilarTracks { get; set; } = [];
}

/// <summary>
/// Represents a pairing between a user and a track.
/// </summary>
public sealed record TrackUserPair
{
    /// <summary>
    /// The unique identifier of the track.
    /// </summary>
    public Guid TrackId { get; set; }

    /// <summary>
    /// The unique identifier of the user associated with the track.
    /// </summary>
    public Guid TrackOwner { get; set; }
}

