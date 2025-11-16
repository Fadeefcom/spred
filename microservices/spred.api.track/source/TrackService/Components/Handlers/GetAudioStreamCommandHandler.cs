using Extensions.Utilities;
using MediatR;
using TrackService.Abstractions;
using TrackService.Models.Commands;

namespace TrackService.Components.Handlers;

/// <summary>
/// Handles the command to get an audio stream for a specific track.
/// </summary>
public class GetAudioStreamCommandHandler : IRequestHandler<GetAudioStreamCommand, Stream?>
{
    private readonly ITrackManager _trackManager;
    private readonly IUploadTrackService _uploadTrackService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAudioStreamCommandHandler"/> class.
    /// </summary>
    /// <param name="trackManager">The repository for track metadata items.</param>
    /// <param name="uploadTrackService">The service for uploading tracks.</param>
    public GetAudioStreamCommandHandler(ITrackManager trackManager,
        IUploadTrackService uploadTrackService)
    {
        _trackManager = trackManager;
        _uploadTrackService = uploadTrackService;
    }

    /// <summary>
    /// Handles the command to get an audio stream for a specific track.
    /// </summary>
    /// <param name="request">The command request containing the track ID.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the audio stream.</returns>
    public async Task<Stream?> Handle(GetAudioStreamCommand request, CancellationToken cancellationToken)
    {
        var bucket = request.SpredUserId == Guid.Empty
            ? GuidShortener.GenerateBucketFromGuid(request.TrackId)
            : "00";
        
        var track = await _trackManager.GetByIdAsync(request.TrackId, request.SpredUserId, cancellationToken, bucket);
        if(track is { IsDeleted: false })
            return await _uploadTrackService.GetStream(request.TrackId, cancellationToken);
        
        return null;
    }
}
