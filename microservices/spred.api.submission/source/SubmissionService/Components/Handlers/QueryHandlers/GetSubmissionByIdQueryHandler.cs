using AutoMapper;
using MediatR;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Interfaces;
using Spred.Bus.Abstractions;
using SubmissionService.Models.Entities;
using SubmissionService.Models.Models;
using SubmissionService.Models.Queries;

namespace SubmissionService.Components.Handlers.QueryHandlers;

/// <summary>
/// Handles <see cref="GetSubmissionByIdQuery"/> requests by retrieving
/// a single submission from the artist inbox based on catalog and submission identifiers.
/// </summary>
/// <remarks>
/// This query handler uses the current actor identity to determine the correct partition key,
/// fetches the corresponding <see cref="ArtistInbox"/> entity from the persistence store,
/// and maps it to a <see cref="SubmissionDto"/> for external consumption.
/// </remarks>
public sealed class GetSubmissionByIdQueryHandler : IRequestHandler<GetSubmissionByIdQuery, SubmissionDto?>
{
    private readonly IActorProvider _actor;
    private readonly IPersistenceStore<ArtistInbox, Guid> _artist;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSubmissionByIdQueryHandler"/> class.
    /// </summary>
    /// <param name="actor">The provider for retrieving the current actor identity and metadata.</param>
    /// <param name="artist">The persistence store for <see cref="ArtistInbox"/> entities.</param>
    /// <param name="mapper">The object mapper used to convert entities into DTOs.</param>
    public GetSubmissionByIdQueryHandler(IActorProvider actor, IPersistenceStore<ArtistInbox, Guid> artist, IMapper mapper)
    {
        _actor = actor;
        _artist = artist;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the <see cref="GetSubmissionByIdQuery"/> request asynchronously.
    /// </summary>
    /// <param name="q">
    /// The query containing the catalog identifier and the submission identifier to retrieve.
    /// </param>
    /// <param name="cancellationToken">A token to observe cancellation requests.</param>
    /// <returns>
    /// A <see cref="SubmissionDto"/> representing the requested submission if found;
    /// otherwise, <c>null</c>.
    /// </returns>
    public async Task<SubmissionDto?> Handle(GetSubmissionByIdQuery q, CancellationToken cancellationToken)
    {
        var userId = _actor.GetActorId();
        var result = await _artist.GetAsync(q.Id, new PartitionKey(userId.ToString()), cancellationToken);
        return _mapper.Map<SubmissionDto?>(result.Result);
    }
}