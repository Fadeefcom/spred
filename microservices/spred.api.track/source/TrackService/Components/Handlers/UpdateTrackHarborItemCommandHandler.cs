using Exception.Exceptions;
using Extensions.Utilities;
using MediatR;
using TrackService.Abstractions;
using TrackService.Models.Commands;

namespace TrackService.Components.Handlers;

/// <summary>
/// Handles the update of track metadata items.
/// </summary>
/// <param name="trackManager">The repository for track metadata items.</param>
public sealed class UpdateTrackServiceItemCommandHandler(
    ITrackManager trackManager)
    : INotificationHandler<UpdateTrackMetadataItemCommand>
{
    /// <summary>
    /// Handles the update track metadata item command.
    /// </summary>
    /// <param name="notification">The update track metadata item command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="BaseException">Thrown when the catalog item is invalid or the update fails.</exception>
    public async Task Handle(UpdateTrackMetadataItemCommand notification, CancellationToken cancellationToken)
    {
        var bucket = notification.SpredUserId == Guid.Empty
            ? GuidShortener.GenerateBucketFromGuid(notification.Id)
            : "00";
        var item = await trackManager.GetByIdAsync(notification.Id, notification.SpredUserId, cancellationToken, bucket);

        if (item is { IsDeleted: false } && item.SpredUserId == notification.SpredUserId)
        {
            item.Update(notification);
            await trackManager.UpdateAsync(item, cancellationToken);
        }
    }
}
