namespace InferenceService.Models.Dto;

/// <summary>
/// Update rate request model
/// </summary>
public sealed record UpdateRateRequest
{
    /// <summary>
    /// Model version
    /// </summary>
    public required string ModelVersion { get; init; }
    
    /// <summary>
    /// Is Liked
    /// </summary>
    public bool? IsLiked { get; init; } = null;
    
    /// <summary>
    /// Is applied
    /// </summary>
    public bool? HasApplied { get; init; } = null;
    
    /// <summary>
    /// Is accepted
    /// </summary>
    public bool? WasAccepted { get; init; } = null;
}