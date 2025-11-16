using MediatR;
using PlaylistService.Abstractions;
using PlaylistService.Models.Entities;
using PlaylistService.Models.Queries;

namespace PlaylistService.Components.Handlers;

/// <summary>
/// Handles the retrieval of user playlists.
/// </summary>
/// <param name="managerPlaylist">The manager responsible for playlist operations.</param>
public class GetCatalogMetadataQueryHandler(IManager managerPlaylist) : IRequestHandler<GetCatalogMetadataQuery, List<CatalogMetadata>>
{
    /// <summary>
    /// Handles the GetUserPlaylistQuery request.
    /// </summary>
    /// <param name="request">The request containing the query parameters and user ID.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the total count and a list of playlist metadata.</returns>
    public async Task<List<CatalogMetadata>> Handle(GetCatalogMetadataQuery request, CancellationToken cancellationToken)
    { 
        var result = await managerPlaylist.GetAsync(request.Query, request.Type, request.SpredUserId, cancellationToken);
        return result.ToList();
    }
}
