using System.Text.Json.Serialization;
using AggregatorService.Components;
using MediatR;

namespace AggregatorService.Models.Commands;

/// <summary>
/// Command to fetch raw audio tracks from YouTube.
/// </summary>
/// <remarks>
/// This command is used to request the raw audio content of tracks from YouTube by providing a list of URLs.
/// The response will include the track's metadata and raw audio content.
/// </remarks>
public sealed record FetchTrackCommand : INotification
{

    public Guid Id { get; init; }
    
    public required string Prompt { get; init; }

    public required string PrimaryId { get; set; }
}
