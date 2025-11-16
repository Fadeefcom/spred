using Xabe.FFmpeg;

namespace InferenceService.Abstractions;

/// <summary>
/// Represents an abstraction over the FFmpeg media analysis functionality.
/// This interface is used to retrieve media information for audio/video files,
/// enabling easier testing and separation of concerns.
/// </summary>
public interface IFFmpegWrapper
{
    /// <summary>
    /// Asynchronously retrieves media information from the given file path.
    /// </summary>
    /// <param name="filePath">The full path to the media file to analyze.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation. 
    /// The result contains <see cref="IMediaInfo"/> with details about the media file.
    /// </returns>
    Task<IMediaInfo> GetMediaInfo(string filePath, CancellationToken cancellationToken);
    
    /// <summary>
    /// Create FFmpeg Conversion
    /// </summary>
    /// <returns>IConversion</returns>
    IConversion CreateConversion();
}