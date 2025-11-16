using MediatR;
using PlaylistService.Models.Entities;

namespace PlaylistService.Models.Queries;

/// <summary>
/// Get playlists command
/// </summary>
public sealed record GetCatalogMetadataQuery : IRequest<List<CatalogMetadata>>
{
    /// <summary>
    /// Gets the user ID associated with the playlists.
    /// </summary>
    public required Guid SpredUserId { get; init; }

    /// <summary>
    /// Gets the query parameters for the request.
    /// </summary>
    public required Dictionary<string, string> Query { get; init; }
    
    /// <summary>
    /// Metadata type
    /// </summary>
    public required string Type { get; init; }
}