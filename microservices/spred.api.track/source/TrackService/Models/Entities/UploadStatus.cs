namespace TrackService.Models.Entities;

/// <summary>
/// Represent upload status events
/// </summary>
public enum UploadStatus
{
    /// <summary>
    /// Pending status
    /// </summary>
    Pending,
    
    /// <summary>
    /// Created status
    /// </summary>
    Created,
    
    /// <summary>
    /// If upload failed
    /// </summary>
    Failed,
    
    /// <summary>
    /// If file deleted from blob
    /// </summary>
    Deleted
}