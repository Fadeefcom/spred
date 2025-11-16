using Newtonsoft.Json;

namespace InferenceService.Models.Dto;

/// <summary>
/// SimilarityResult
/// </summary>
public class SimilarityResultDto
{
    /// <summary>
    /// ID
    /// </summary>
    [JsonProperty("id")]
    public Guid Id { get; set; }
    
    /// <summary>
    /// Track url
    /// </summary>
    [JsonProperty("SpredUserId")]
    public Guid SpredUserId { get; set; }
    
    /// <summary>
    /// Track Id
    /// </summary>
    [JsonProperty("TrackId")]
    public Guid TrackId { get; set; }
    
    /// <summary>
    /// Similarity
    /// </summary>
    [JsonProperty("Similarity")]
    public float Similarity  { get; set; }
}