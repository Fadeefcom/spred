using System.Linq.Expressions;
using AutoMapper;
using Extensions.Extensions;
using MediatR;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Interfaces;
using Spred.Bus.Abstractions;
using SubmissionService.Models.Entities;
using SubmissionService.Models.Models;
using SubmissionService.Models.Queries;

namespace SubmissionService.Components.Handlers.QueryHandlers;

/// <summary>
/// Handles <see cref="GetSubmissionsByCatalogQuery"/> requests by retrieving
/// submissions from the persistence store for a given catalog and curator.
/// </summary>
/// <remarks>
/// This query handler filters submissions by catalog identifier and optionally by status.
/// It uses the current actor identity as the curator, constructs the appropriate partition key,
/// and maps the retrieved entities into <see cref="SubmissionDto"/> results.
/// </remarks>
public sealed class GetSubmissionsByCatalogQueryHandler : IRequestHandler<GetSubmissionsByCatalogQuery, IEnumerable<SubmissionDto>>
{
    private readonly IActorProvider _actor;
    private readonly IPersistenceStore<Submission, Guid> _submission;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSubmissionsByCatalogQueryHandler"/> class.
    /// </summary>
    /// <param name="actor">The provider for retrieving the current actor identity and metadata.</param>
    /// <param name="submission">The persistence store for <see cref="Submission"/> entities.</param>
    /// <param name="mapper">The object mapper used to convert entities into DTOs.</param>
    public GetSubmissionsByCatalogQueryHandler(IActorProvider actor, IPersistenceStore<Submission, Guid> submission, IMapper mapper)
    {
        _actor = actor;
        _submission = submission;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the <see cref="GetSubmissionsByCatalogQuery"/> request asynchronously.
    /// </summary>
    /// <param name="q">
    /// The query containing catalog identifier, optional status filter,
    /// and paging parameters (offset and limit).
    /// </param>
    /// <param name="cancellationToken">A token to observe cancellation requests.</param>
    /// <returns>
    /// A collection of <see cref="SubmissionDto"/> representing the submissions
    /// matching the query criteria. If no results are found, an empty collection is returned.
    /// </returns>
    public async Task<IEnumerable<SubmissionDto>> Handle(GetSubmissionsByCatalogQuery q, CancellationToken cancellationToken)
    {
        var curatorUserId = _actor.GetActorId();
        var partitionKey = new PartitionKeyBuilder().Add(curatorUserId.ToString()).Add(q.CatalogItemId.ToString()).Build();
        Expression<Func<Submission, bool>> predicate = x => x.Type.Equals("Submission");
        if (q.Status is not null) predicate = predicate.And(x => x.Status.Equals(q.Status));
        var result = await _submission.GetAsync(predicate, s => s.Timestamp, partitionKey, q.Offset, q.Limit, false, cancellationToken: cancellationToken);
        return _mapper.Map<List<SubmissionDto>>(result.Result ?? []);
    }
}