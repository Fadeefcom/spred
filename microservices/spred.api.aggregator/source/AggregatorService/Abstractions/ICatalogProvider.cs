using AggregatorService.Models.Dto;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;

namespace AggregatorService.Abstractions;

/// <summary>
/// Provides an abstraction for interacting with catalog data from various platforms.
/// This interface outlines methods to resolve playlist identifiers, retrieve metadata, fetch statistics,
/// and acquire snapshots of tracks belonging to a specific playlist.
/// </summary>
public interface ICatalogProvider
{
    /// Resolves a unique playlist identifier based on the provided primary identifier and platform.
    /// <param name="primaryId">The primary identifier that represents the playlist across different platforms.</param>
    /// <param name="platform">The platform for which the playlist is being resolved (e.g., Spotify, Apple Music).</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the resolved playlist identifier, or null if no match is found.</returns>
    Task<string?> ResolvePlaylistIdAsync(string primaryId, string platform);

    /// Asynchronously retrieves metadata for a given playlist on a specific platform.
    /// <param name="playlistId">
    /// The unique identifier of the playlist for which to retrieve metadata.
    /// </param>
    /// <param name="platform">
    /// The platform on which the playlist exists (e.g., Spotify, Apple Music).
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains a MetadataDto object
    /// representing the playlist metadata, or null if the metadata could not be retrieved.
    /// </returns>
    Task<MetadataDto?> GetPlaylistMetadataAsync(string playlistId, string platform);

    /// Retrieves statistical information about a specific playlist from the catalog.
    /// <param name="playlistId">The unique identifier of the playlist whose statistics are being retrieved.</param>
    /// <param name="platform">The platform identifier (e.g., Spotify, Apple Music) for the playlist.</param>
    /// <param name="updateStats">A boolean flag indicating whether the playlist statistics should be updated before retrieval.</param>
    /// <returns>A task that represents an asynchronous operation and contains a hash set of statistical details for the playlist.</returns>
    Task<HashSet<StatInfo>> GetPlaylistStatsAsync(string playlistId, string platform, bool updateStats);

    /// Retrieves a snapshot of tracks from a playlist for a specific platform at a given report date.
    /// <param name="playlistId">The unique identifier of the playlist.</param>
    /// <param name="platform">The platform for which the tracks are being retrieved.</param>
    /// <param name="reportDate">The date for which the tracks snapshot is requested.</param>
    /// <returns>A list of track information, including platform-specific identifiers, or an empty list if no tracks are found.</returns>
    Task<List<TrackDtoWithPlatformIds>> GetPlaylistTracksSnapshotAsync(string playlistId, string platform, DateTime reportDate);

    /// Retrieves metadata information for a specific radio station based on the provided unique identifier (slug).
    /// <param name="slug">The unique identifier of the radio station whose metadata is to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the radio station as a MetadataDto object, or null if no metadata is found.</returns>
    Task<RadioInfo?> GetRadioMetadataAsync(string slug);

    /// Retrieves a snapshot of radio tracks limited by a specified number of tracks.
    /// <param name="slug">The unique identifier or slug representing the radio source.</param>
    /// <param name="trackLimit">The maximum number of tracks to include in the snapshot.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a set of statistical information about the radio tracks.</returns>
    Task<(List<TrackDtoWithPlatformIds>, int total)> GetRadioTracksSnapshotAsync(string slug, int trackLimit);

    /// Retrieves a list of platforms where a specific radio station is available, along with associated details such as primary identifier and URL.
    /// <param name="slug">The unique slug identifying the radio station.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains a list of tuples, where each tuple includes
    /// the platform name, the primary identifier for the platform, and the URL associated with the radio on the platform.
    /// </returns>
    Task<List<(string Platform, string PrimaryId, string Url)>> GetRadioPlatforms(string slug);
}