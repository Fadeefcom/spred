using Exception;
using Extensions.Utilities;
using MediatR;
using PlaylistService.Abstractions;
using PlaylistService.Models.Commands;

namespace PlaylistService.Components.Handlers;

/// <summary>
/// Handles the update playlist command.
/// </summary>
public sealed class UpdateMetadataCommandHandler : INotificationHandler<UpdateMetadataCommand>
{
    private readonly IManager _manager;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateMetadataCommandHandler"/> class.
    /// </summary>
    /// <param name="manager">The manager responsible for playlist operations.</param>
    public UpdateMetadataCommandHandler(IManager manager)
    {
        _manager = manager;
    }

    /// <summary>
    /// Handles the update playlist command.
    /// </summary>
    /// <param name="notification">The update playlist command containing the updated playlist information.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task Handle(UpdateMetadataCommand notification, CancellationToken cancellationToken)
    {
        var bucket = notification.SpredUserId == Guid.Empty
            ? GuidShortener.GenerateBucketFromGuid(notification.Id)
            : "00";
        
        var item = await _manager.FindByIdAsync(notification.Id,  notification.SpredUserId, cancellationToken, bucket);
        item.ThrowBaseExceptionIfNull("PlaylistService not found", status: (int)ErrorCode.NotFound,
            "Invalid playlist item information");

        if (item is { IsDeleted: false} && item.SpredUserId == notification.SpredUserId)
        {
            item.Update(notification);
            await _manager.UpdateAsync(item, cancellationToken);
        }
    }
}
