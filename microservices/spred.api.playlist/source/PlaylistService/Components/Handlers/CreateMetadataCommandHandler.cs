using Exception;
using Exception.Exceptions;
using MediatR;
using PlaylistService.Abstractions;
using PlaylistService.Models.Commands;
using PlaylistService.Models.Entities;

namespace PlaylistService.Components.Handlers;

/// <summary>
/// Handles the creation of a new playlist.
/// </summary>
public sealed class CreateMetadataCommandHandler : IRequestHandler<CreateMetadataCommand, Guid>
{
    private readonly IManager _manager;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateMetadataCommandHandler"/> class.
    /// </summary>
    /// <param name="manager">The manager responsible for playlist operations.</param>
    public CreateMetadataCommandHandler(IManager manager)
    {
        _manager = manager;
    }

    /// <summary>
    /// Handles the creation of a new playlist.
    /// </summary>
    /// <param name="notification">The command containing the playlist details.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The ID of the newly created playlist.</returns>
    /// <exception cref="BaseException">Thrown when the playlist could not be created.</exception>
    public async Task<Guid> Handle(CreateMetadataCommand notification, CancellationToken cancellationToken)
    {
        CatalogMetadata item = notification.Type switch
        {
            "playlist" => new PlaylistMetadata(),
            "record" => new RecordLabelMetadata(),
            "radio" => new RadioMetadata(),
            "" => new PlaylistMetadata(),
            null =>  new PlaylistMetadata(),
            _ => throw new NotSupportedException($"Unknown type: {notification.Type}")
        };
        
        if (!string.IsNullOrWhiteSpace(notification.PrimaryId))
        {
            var ifExists =
                await _manager.ExistsByPrimaryIdAsync(notification.PrimaryId, notification.SpredUserId,
                    CancellationToken.None);
            if (ifExists.HasValue)
                return ifExists.Value;
        }
        
        item.Create(notification);
        int attempts = 3;
        bool result = false;

        while (attempts > 0)
        {
            result = await _manager.AddAsync(item, cancellationToken);
            
            if (result)
                break;
            attempts--;
        }

        if (!result)
            throw new BaseException("Can't add playlist",
                (int)ErrorCode.Conflict,
                "Playlist could not be created",
                $"nodeId: {Environment.MachineName}");

        return item.Id;
    }
}
