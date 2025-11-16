using MediatR;

namespace TrackService.Models.Commands;

/// <summary>
/// Command to get the audio stream for a specific track.
/// </summary>
public sealed record GetAudioStreamCommand : IRequest<Stream?>
{
    /// <summary>
    /// Gets the unique identifier of the track.
    /// </summary>
    public Guid TrackId { get; init; }
    
    /// <summary>
    /// Gets the unique identifier of the user.
    /// </summary>
    public Guid SpredUserId { get; init; }
}
