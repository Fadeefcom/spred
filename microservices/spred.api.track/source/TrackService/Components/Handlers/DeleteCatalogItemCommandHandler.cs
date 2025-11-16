using Exception.Exceptions;
using Extensions.Utilities;
using MediatR;
using TrackService.Abstractions;
using TrackService.Models.Commands;

namespace TrackService.Components.Handlers;

/// <summary>
/// Handles the deletion of a catalog item in the track service.
/// </summary>
/// <param name="trackManager">The repository for track metadata items.</param>
public class DeleteCatalogItemCommandHandler(
    ITrackManager trackManager)
    : INotificationHandler<DeleteTrackMetadataItemCommand>
{
    /// <summary>
    /// Handles the deletion of a track metadata item.
    /// </summary>
    /// <param name="notification">The command containing the track metadata item ID and user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="BaseException">Thrown when the track metadata item is not found or already deleted.</exception>
    public async Task Handle(DeleteTrackMetadataItemCommand notification, CancellationToken cancellationToken)
    {
        var bucket = notification.SpredUserId == Guid.Empty
            ? GuidShortener.GenerateBucketFromGuid(notification.TrackMetadataId)
            : "00";
        await trackManager.DeleteAsync(notification.TrackMetadataId, notification.SpredUserId, cancellationToken, bucket);
    }
}
