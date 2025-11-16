using AggregatorService.Models.Commands;

namespace AggregatorService.Abstractions;

/// <summary>
/// Service responsible for preloading and supplying track download commands
/// from Cosmos DB for later YouTube processing.
/// </summary>
public interface ITrackDownloadService
{
    /// <summary>
    /// Returns a pre-fetched <see cref="FetchTrackCommand"/> if available,
    /// triggering background refill if the cache drops below a threshold.
    /// </summary>
    /// <returns>A <see cref="FetchTrackCommand"/> if one is available; otherwise, <c>null</c>.</returns>
    public FetchTrackCommand? GetTrackFromYoutubeCommand();
}