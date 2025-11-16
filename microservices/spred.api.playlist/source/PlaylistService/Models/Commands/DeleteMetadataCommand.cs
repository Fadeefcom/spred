using MediatR;

namespace PlaylistService.Models.Commands;

/// <summary>
/// Command to delete a playlist.
/// </summary>
public sealed record DeleteMetadataCommand : IRequest<bool>
{
    /// <summary>
    /// Gets or sets the unique identifier of the playlist to be deleted.
    /// </summary>
    public required Guid PlaylistId { get; init; }

    /// <summary>
    /// Gets or sets the unique identifier of the user requesting the deletion.
    /// </summary>
    public required Guid SpredUserId { get; init; }
}
