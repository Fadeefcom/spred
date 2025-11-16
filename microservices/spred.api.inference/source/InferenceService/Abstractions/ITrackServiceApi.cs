using Refit;

namespace InferenceService.Abstractions;

/// <summary>
/// Interface for interacting with the track service API.
/// </summary>
public interface ITrackServiceApi
{
    /// <summary>
    /// Retrieves the audio track by its identifier.
    /// </summary>
    /// <param name="spredUserId">The user id associated with a track.</param>
    /// <param name="id">The identifier of the track.</param>
    /// <returns>A stream containing the audio track.</returns>
    [Get("/internal/track/audio/{spredUserId}/{id}")]
    public Task<IApiResponse<Stream>> GetTrackStreamById(string spredUserId, string id);
}
