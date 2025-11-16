using Exception.Exceptions;
using Extensions.Utilities;
using MediatR;
using TrackService.Abstractions;
using TrackService.Models.Entities;
using TrackService.Models.Queries;

namespace TrackService.Components.Handlers;

/// <summary>
/// Handles the retrieval of track metadata items.
/// </summary>
public class GetTrackMetadataItemCommandHandler : IRequestHandler<GetTrackMetadataItemCommand, TrackMetadata?>
{
    private readonly ITrackManager _trackManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetTrackMetadataItemCommandHandler"/> class.
    /// </summary>
    /// <param name="trackManager">The repository to access track metadata items.</param>
    public GetTrackMetadataItemCommandHandler(ITrackManager trackManager)
    {
        _trackManager = trackManager;
    }

    /// <summary>
    /// Handles the request to get a track metadata item.
    /// </summary>
    /// <param name="request">The request containing the track metadata ID and user ID.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The track metadata item if found.</returns>
    /// <exception cref="BaseException">Thrown when the track metadata item is not found or is deleted.</exception>
    public async Task<TrackMetadata?> Handle(GetTrackMetadataItemCommand request, CancellationToken cancellationToken)
    {
        var bucket = request.SpredUserId == Guid.Empty
            ? GuidShortener.GenerateBucketFromGuid(request.TrackMetadataId)
            : "00";
        
        var result =
            await _trackManager.GetByIdAsync(request.TrackMetadataId, request.SpredUserId, cancellationToken, bucket);

        return result is { IsDeleted: true } ? null : result;
    }
}
