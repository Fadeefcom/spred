using MediatR;
using PlaylistService.Models.Entities;

namespace PlaylistService.Models.Queries;

/// <summary>
/// Query command
/// </summary>
public sealed record GetMetadataQueryByIds : IRequest<List<CatalogMetadata>>
{
    /// <summary>
    /// Metadata ids
    /// </summary>
    public required Dictionary<Guid, Guid> OwnerMetadataIds { get; init; }
    
    /// <summary>
    /// Metadata filter type
    /// </summary>
    public required string Type {get; init; }
}