using Extensions.Utilities;
using MediatR;
using PlaylistService.Abstractions;
using PlaylistService.Models.Entities;
using PlaylistService.Models.Queries;

namespace PlaylistService.Components.Handlers;

/// <summary>
/// Handler for retrieving a playlist by its ID.
/// </summary>
public class GetMetadataByIdHandler : IRequestHandler<GetMetadataByIdQuery, (CatalogMetadata?, int)>
{
    private readonly IManager _manager;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMetadataByIdHandler"/> class.
    /// </summary>
    /// <param name="managerPlaylist">The manager responsible for playlist operations.</param>
    public GetMetadataByIdHandler(IManager managerPlaylist)
    {
        _manager = managerPlaylist;
    }

    /// <summary>
    /// Handles the request to get a playlist by its ID.
    /// </summary>
    /// <param name="request">The query containing the playlist ID.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The playlist metadata if found and is public; otherwise, null.</returns>
    public async Task<(CatalogMetadata?, int)> Handle(GetMetadataByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.PlaylistId == Guid.Empty)
            return (null, -1);
        
        var bucket = request.SpredUserId == Guid.Empty
            ? GuidShortener.GenerateBucketFromGuid(request.PlaylistId)
            : "00";
        
        if (request.IncludeStatistics)
        {
            var findTask = _manager.FindByIdAsync(request.PlaylistId, request.SpredUserId, cancellationToken, bucket);
            var statsTask = _manager.GetStatisticDifference(request.PlaylistId, cancellationToken);
            await Task.WhenAll(findTask, statsTask);
            
            var result = await findTask;
            var stats = await statsTask;

            return (result, stats);
        }
        else
        {
            var findTask = await _manager.FindByIdAsync(request.PlaylistId, request.SpredUserId, cancellationToken, bucket);
            return (findTask, -1);
        }
    }
}
