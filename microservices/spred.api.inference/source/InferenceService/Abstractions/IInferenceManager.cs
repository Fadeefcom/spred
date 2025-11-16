using InferenceService.Models.Dto;
using InferenceService.Models.Entities;

namespace InferenceService.Abstractions;

/// <summary>
/// Interface for managing inference data.
/// </summary>
public interface IInferenceManager
{
    /// <summary>
    /// Saves inference results.
    /// </summary>
    /// <param name="results">The inference results as a InferenceMetadata.</param>
    /// <param name="trackId">The unique identifier for the track.</param>
    /// <param name="spredUserId">User ID</param>
    /// <param name="modelVersion">The version of the model.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task<InferenceResult> SaveInference(List<InferenceMetadata> results, Guid trackId, Guid spredUserId, string modelVersion, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves inference results.
    /// </summary>
    /// <param name="trackId">The unique identifier for the track.</param>
    /// <param name="spredUserId">User ID</param>
    /// <param name="isPremium">Is premium user.</param>
    /// <param name="modelVersion">The version of the model.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task representing the asynchronous operation, with a tuple containing the model version and the inference results as a list.</returns>
    public Task<(string, Guid, List<InferenceMetadataDto>?)> GetInference(Guid trackId, Guid spredUserId, bool isPremium, string modelVersion, CancellationToken cancellationToken);

    /// <summary>
    /// Updates existing inference results.
    /// </summary>
    /// <param name="results">The updated inference results as a dictionary.</param>
    /// <param name="trackId">The unique identifier for the track.</param>
    /// <param name="spredUserId">User ID</param>
    /// <param name="modelVersion">The version of the model.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task UpdateInference(Dictionary<string, (string, float)> results, Guid trackId, Guid spredUserId, string modelVersion, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a rating to a playlist.
    /// </summary>
    /// <param name="playlistId">The unique identifier for the playlist.</param>
    /// <param name="trackId">The unique identifier for the track.</param>
    /// <param name="spredUserId">User ID</param>
    /// <param name="modelVersion">The version of the model.</param>
    /// <param name="reaction">The rating to be added.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task AddRateToPlaylist(Guid playlistId, Guid trackId, Guid spredUserId, string modelVersion, ReactionStatus reaction, CancellationToken cancellationToken);

    /// <summary>
    /// Update inference
    /// </summary>
    /// <param name="id"></param>
    /// <param name="trackId"></param>
    /// <param name="similarTrack"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task UpdateInference(Guid id, Guid trackId, Dictionary<TrackMetadataPair, List<SimilarTrack>> similarTrack, CancellationToken cancellationToken);
}
