using AutoMapper;
using MediatR;
using Spred.Bus.DTOs;
using TrackService.Abstractions;
using TrackService.Models;
using TrackService.Models.DTOs;
using TrackService.Models.Queries;

namespace TrackService.Components.Handlers;

/// <summary>
/// Handles the command to get track metadata by query.
/// </summary>
public class GetQueryTrackServiceItemCommandHandler : IRequestHandler<GetTrackMetadataByQueryCommand, TracksResponseModel>
{
    private readonly ITrackManager _trackManager;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetQueryTrackServiceItemCommandHandler"/> class.
    /// </summary>
    /// <param name="trackManager">The repository to access track metadata items.</param>
    /// <param name="mapper">The mapper to convert entities to DTOs.</param>
    public GetQueryTrackServiceItemCommandHandler(ITrackManager trackManager, IMapper mapper)
    {
        _trackManager = trackManager;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the command to get track metadata by query.
    /// </summary>
    /// <param name="request">The command containing query parameters and user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the tracks response model.</returns>
    public async Task<TracksResponseModel> Handle(GetTrackMetadataByQueryCommand request, CancellationToken cancellationToken)
    {
        var result = await _trackManager.GetAsync(request.QueryParams, request.SpredUserId, cancellationToken);

        return new TracksResponseModel()
        {
            Tracks = _mapper.Map<List<PrivateTrackDto>>(result.ToList()),
            Total = 0
        };
    }
}
