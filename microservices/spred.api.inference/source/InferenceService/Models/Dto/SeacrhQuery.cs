using System.Text.Json.Serialization;

namespace InferenceService.Models.Dto;

/// <summary>
/// Represents a search query with vector embedding for similarity search.
/// </summary>
public record SearchQuery
{
    /// <summary>
    /// Vector embedding used for similarity comparison.
    /// </summary>
    public required float[] Embedding { get; init; }

    /// <summary>
    /// Similarity threshold used for similarity filter.
    /// </summary>
    [JsonPropertyName("similarity_threshold")]
    public double? SimilarityThreshold { get; init; }
}