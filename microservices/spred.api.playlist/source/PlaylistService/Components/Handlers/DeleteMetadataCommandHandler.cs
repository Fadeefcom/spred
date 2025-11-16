using MediatR;
using PlaylistService.Abstractions;
using PlaylistService.Models.Commands;

namespace PlaylistService.Components.Handlers;

/// <summary>
/// Handles the deletion of a playlist.
/// </summary>
public sealed class DeleteMetadataCommandHandler : IRequestHandler<DeleteMetadataCommand, bool>
{
    private readonly IManager _manager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteMetadataCommandHandler"/> class.
    /// </summary>
    /// <param name="manager">The manager responsible for playlist operations.</param>
    public DeleteMetadataCommandHandler(IManager manager)
    {
        _manager = manager;
    }

    /// <summary>
    /// Handles the delete playlist command.
    /// </summary>
    /// <param name="notification">The delete playlist command notification.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task<bool> Handle(DeleteMetadataCommand notification, CancellationToken cancellationToken)
    {
        var item = await _manager.FindByIdAsync(notification.PlaylistId, notification.SpredUserId, cancellationToken);

        if (item is { IsDeleted: false } && item.SpredUserId == notification.SpredUserId)
        {
            item.Delete();
            return await _manager.UpdateAsync(item, cancellationToken);
        }
        
        return false;
    }
}
