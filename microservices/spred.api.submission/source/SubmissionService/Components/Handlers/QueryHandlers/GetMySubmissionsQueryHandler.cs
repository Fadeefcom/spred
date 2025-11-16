using System.Linq.Expressions;
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
/// Handles <see cref="GetMySubmissionsQuery"/> requests by retrieving
/// submissions for the current actor, either as an artist or as a curator.
/// </summary>
/// <remarks>
/// The query handler determines the role of the current actor from the <see cref="IActorProvider"/>.
/// If the role is "Artist", it queries the artist inbox for submissions.
/// If the role is "Curator", it queries submissions directly from the curator's perspective.
/// The results are filtered by optional status and paginated according to offset and limit.
/// </remarks>
public sealed class GetMySubmissionsQueryHandler : IRequestHandler<GetMySubmissionsQuery, IEnumerable<SubmissionDto>>
{
    private readonly IActorProvider _actor;
    private readonly IPersistenceStore<ArtistInbox, Guid> _artist;
    private readonly IPersistenceStore<Submission, Guid> _submission;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMySubmissionsQueryHandler"/> class.
    /// </summary>
    /// <param name="actor">The provider for retrieving the current actor identity and metadata.</param>
    /// <param name="artist">The persistence store for <see cref="ArtistInbox"/> entities.</param>
    /// <param name="submission">The persistence store for <see cref="Submission"/> entities.</param>
    /// <param name="mapper">The object mapper used to convert entities into DTOs.</param>
    public GetMySubmissionsQueryHandler(IActorProvider actor, IPersistenceStore<ArtistInbox, Guid> artist, IPersistenceStore<Submission, Guid> submission, IMapper mapper)
    {
        _actor = actor;
        _artist = artist;
        _submission = submission;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the <see cref="GetMySubmissionsQuery"/> request asynchronously.
    /// </summary>
    /// <param name="q">
    /// The query specifying an optional submission status filter and pagination parameters.
    /// </param>
    /// <param name="cancellationToken">A token to observe cancellation requests.</param>
    /// <returns>
    /// A collection of <see cref="SubmissionDto"/> representing the submissions
    /// associated with the current actor, filtered and paginated as requested.
    /// If no submissions are found, an empty collection is returned.
    /// </returns>
    public async Task<IEnumerable<SubmissionDto>> Handle(GetMySubmissionsQuery q, CancellationToken cancellationToken)
    {
        var role = _actor.GetRoleName();
        var curatorUserId = _actor.GetActorId();
        var partitionKey = new PartitionKeyBuilder().Add(curatorUserId.ToString()).Build();

        if (role.Equals("Artist", StringComparison.Ordinal))
        {
            Expression<Func<ArtistInbox, bool>> predicate = q.Status is not null ? x => x.Status.Equals(q.Status) : x => true;
            var result = await _artist.GetAsync(predicate, s => s.Timestamp, partitionKey, q.Offset, q.Limit, false, cancellationToken: cancellationToken);
            return _mapper.Map<List<SubmissionDto>>(result.Result ?? []);
        }
        else if (role.Equals("Curator", StringComparison.Ordinal))
        {
            Expression<Func<Submission, bool>> predicate2 = x => x.Type.Equals("Submission");
            if (q.Status is not null) predicate2 = x => x.Status.Equals(q.Status);
            var result = await _submission.GetAsync(predicate2, s => s.Timestamp, partitionKey, q.Offset, q.Limit, false, cancellationToken: cancellationToken);
            return _mapper.Map<List<SubmissionDto>>(result.Result ?? []);
        }

        return [];
    }
}