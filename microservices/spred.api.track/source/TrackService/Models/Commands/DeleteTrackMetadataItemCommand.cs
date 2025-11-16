using MediatR;

namespace TrackService.Models.Commands;

/// <summary>
/// Command to delete a track metadata item.
/// </summary>
/// <param name="id">The unique identifier of the track metadata item to be deleted.</param>
/// <param name="SpredUserId">The unique identifier of the user performing the delete operation.</param>
public class DeleteTrackMetadataItemCommand(Guid id, Guid SpredUserId) : INotification
{
    /// <summary>
    /// Gets the unique identifier of the track metadata item to be deleted.
    /// </summary>
    public Guid TrackMetadataId { get; private set; } = id;

    /// <summary>
    /// Gets the unique identifier of the user performing the delete operation.
    /// </summary>
    public Guid SpredUserId { get; private set; } = SpredUserId;
}

