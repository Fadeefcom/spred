namespace AggregatorService.Abstractions;

/// <summary>
/// Service responsible for uploading audio files and reporting failed track processing
/// to the TrackService API.
/// </summary>
public interface ITrackSenderService
{
    
    /// <summary>
    /// Uploads an audio track file to the TrackService associated with the specified track ID.
    /// The file is temporarily stored and deleted after upload.
    /// </summary>
    /// <param name="file">The uploaded form file representing the audio content.</param>
    /// <param name="id">The internal track identifier.</param>
    /// <returns>A task that represents the asynchronous upload operation.</returns>
    public Task PushTrack(IFormFile file, Guid id);

    /// <summary>
    /// Notifies the TrackService that processing a track failed for the given track ID.
    /// </summary>
    /// <param name="id">The track ID for which processing failed.</param>
    /// <returns>A task that completes when the request is enqueued.</returns>
    public Task UnsuccessfulResult(Guid id);
}