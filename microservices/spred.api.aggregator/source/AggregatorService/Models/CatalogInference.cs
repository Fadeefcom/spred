namespace AggregatorService.Models;

/// <summary>
/// Represents the result of catalog enrichment and inference processing.
/// </summary>
public record CatalogInference
{
    /// <summary>
    /// Unique identifier for the inference record.
    /// </summary>
    public Guid id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Type of catalog data being processed (e.g., playlistMetadata).
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Bucket identifier used for partitioning or batch processing.
    /// </summary>
    public int Bucket { get; set; }

    /// <summary>
    /// UTC timestamp when the enrichment was performed.
    /// </summary>
    public DateTime EnrichedAt = DateTime.UtcNow;

    /// <summary>
    /// Human-readable date string for the enrichment run (e.g., "2025-07-01").
    /// </summary>
    public string EnrichedDate { get; set; }

    /// <summary>
    /// Processing status. 0 = not processed, 1 = processed.
    /// </summary>
    public ushort Status { get; set; }

    /// <summary>
    /// List of catalog inference responses generated from this enrichment.
    /// </summary>
    public List<CatalogInferenceResponce> catalogInferenceResponces { get; set; }
}

/// <summary>
/// Represents inference results for a specific catalog.
/// </summary>
public record CatalogInferenceResponce
{
    /// <summary>
    /// Unique identifier of the catalog.
    /// </summary>
    public Guid CatalogId { get; init; }

    /// <summary>
    /// User ID of the catalog owner.
    /// </summary>
    public Guid CatalogOwner { get; init; }

    /// <summary>
    /// List of tracks with inferred metadata.
    /// </summary>
    public List<TrackInference> TrackIdOwner { get; set; }
}

/// <summary>
/// Represents metadata inference for a single track.
/// </summary>
public record TrackInference
{
    /// <summary>
    /// Unique identifier of the track.
    /// </summary>
    public Guid TrackId { get; set; }

    /// <summary>
    /// User ID of the track owner.
    /// </summary>
    public Guid TrackOwner { get; set; }

    /// <summary>
    /// Inferred genre of the track.
    /// </summary>
    public string Genre { get; set; }
}
