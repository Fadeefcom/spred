using TrackService.Models.DTOs;

namespace TrackService.Abstractions;

/// <summary>
/// Provides functionality to analyze a track and retrieve its metadata.
/// </summary>
public interface IAnalayzeTrackService
{
    /// <summary>
    /// Analyzes the specified track file and retrieves its metadata.
    /// </summary>
    /// <param name="filePath">The path to the track file to be analyzed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the analyzed track.</returns>
    public Task<AnalyzeTrackDto> Analayze(string filePath, CancellationToken cancellationToken);
}
