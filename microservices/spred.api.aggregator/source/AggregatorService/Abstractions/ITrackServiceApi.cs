using System.Text.Json;
using Refit;
using Spred.Bus.DTOs;

namespace AggregatorService.Abstractions;

/// <summary>
/// Track harbor endpoint
/// </summary>
public interface ITrackServiceApi
{
    /// <summary>
    /// Publishes a new track.
    /// </summary>
    /// <param name="spredUserId">The user id.</param>
    /// <param name="trackDto">The track metadata.</param>
    /// <returns>The unique identifier of the published track.</returns>
    [Post("/internal/track/{spredUserId}")]
    public Task<IApiResponse<JsonElement>> AddTrack(string spredUserId, [Body] TrackDto trackDto);

    /// <summary>
    /// Publishes a new track.
    /// </summary>
    /// <param name="spredUserId">The user id.</param>
    /// <param name="id">Track id.</param>
    /// <param name="formFile">The track file to be uploaded.</param>
    /// <returns>The unique identifier of the published track.</returns>
    [Multipart]
    [Patch("/internal/track/{spredUserId}/{id}")]
    public Task AddAudioTrack(string spredUserId, string id, [AliasAs("file")]  StreamPart formFile);
    
    /// <summary>
    /// Set track status
    /// </summary>
    /// <param name="spredUserId">The user id.</param>
    /// <param name="id"></param>
    /// <returns></returns>
    [Patch("/internal/track/{spredUserId}/{id}/unsuccessful")]
    public Task UnsuccessfulResult(string spredUserId, string id);

    /// <summary>
    /// Get track audio
    /// </summary>
    /// <param name="spredUserId">The user id.</param>
    /// <param name="id"></param>
    /// <returns></returns>
    [Get("/internal/track/audio/exists/{spredUserId}/{id}")]
    public Task<IApiResponse<HttpResponse>> CheckTrackAudio(string spredUserId, string id);
}
