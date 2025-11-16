namespace InferenceService.Configuration;

/// <summary>
/// Model version
/// </summary>
public record ModelVersion
{
    /// <summary>
    /// Section name
    /// </summary>
    public const string SectionName = "ModelVersion";
    
    /// <summary>
    /// Version
    /// </summary>
    public required string Version { get; init; }
    
    /// <summary>
    /// Threshold for the model
    /// </summary>
    public required double Threshold { get; init; }
}