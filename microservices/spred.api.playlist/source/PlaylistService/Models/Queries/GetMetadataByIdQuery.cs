using MediatR;
using PlaylistService.Models.Entities;

namespace PlaylistService.Models.Queries;

/// <summary>
/// Get playlist query
/// </summary>
public sealed record GetMetadataByIdQuery : IRequest<(CatalogMetadata?, int)>
{
    /// <summary>
    /// Gets the unique identifier of the playlist.
    /// </summary>
    public required Guid PlaylistId { get; init; }
    
    /// <summary>
    /// Spred user ID
    /// </summary>
    public required Guid SpredUserId { get; init; }

    /// <summary>
    /// Flag is statistics required
    /// </summary>
    public required bool IncludeStatistics { get; init; }
}