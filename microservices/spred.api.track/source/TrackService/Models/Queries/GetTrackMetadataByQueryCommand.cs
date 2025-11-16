using MediatR;

namespace TrackService.Models.Queries;

/// <summary>
/// Represents a command to get track metadata by query.
/// </summary>
public sealed record GetTrackMetadataByQueryCommand : IRequest<TracksResponseModel>
{
    /// <summary>
    /// Gets the query parameters.
    /// </summary>
    public required Dictionary<string, string> QueryParams { get; init; }

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    public required Guid SpredUserId { get; init; }
}
