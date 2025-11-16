using MediatR;
using TrackService.Models.Entities;

namespace TrackService.Models.Queries;

/// <summary>
/// Command to get track metadata item
/// </summary>
public class GetTrackMetadataItemCommand : IRequest<TrackMetadata?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetTrackMetadataItemCommand"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the track metadata.</param>
    /// <param name="spredUserId">The unique identifier of the user requesting the metadata.</param>
    public GetTrackMetadataItemCommand(Guid id, Guid spredUserId)
    {
        TrackMetadataId = id;
        SpredUserId = spredUserId;
    }

    /// <summary>
    /// Gets the unique identifier of the track metadata.
    /// </summary>
    public Guid TrackMetadataId { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the user requesting the metadata.
    /// </summary>
    public Guid SpredUserId { get; private set; }
}
