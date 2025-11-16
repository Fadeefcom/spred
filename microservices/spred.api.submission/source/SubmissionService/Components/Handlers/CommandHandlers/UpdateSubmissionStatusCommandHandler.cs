using System.Net;
using Exception.Exceptions;
using MediatR;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Components;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces;
using Spred.Bus.Abstractions;
using SubmissionService.Models.Commands;
using SubmissionService.Models.Entities;

namespace SubmissionService.Components.Handlers.CommandHandlers;

/// <summary>
/// Handles <see cref="UpdateSubmissionStatusCommand"/> requests by updating
/// the status of an existing submission and recording the change in both
/// the submission entity and the artist inbox.
/// </summary>
/// <remarks>
/// This command handler retrieves the target submission and the associated artist inbox entry,
/// validates the requested status update, and persists the changes transactionally in Cosmos DB.
/// It also creates an outbox event for downstream processing and returns a result containing
/// both the old and new status values.
/// </remarks>
public sealed class UpdateSubmissionStatusCommandHandler : IRequestHandler<UpdateSubmissionStatusCommand, SubmissionStatusUpdatedResult>
{
    private readonly Container _container;
    private readonly IActorProvider _actor;
    private readonly IPersistenceStore<Submission, Guid> _submission;
    private readonly IPersistenceStore<ArtistInbox, Guid> _artist;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSubmissionStatusCommandHandler"/> class.
    /// </summary>
    /// <param name="container">The Cosmos DB container used for transactional batch operations.</param>
    /// <param name="actor">The provider for retrieving the current actor identity and correlation metadata.</param>
    /// <param name="submission">The persistence store for <see cref="Submission"/> entities.</param>
    /// <param name="artist">The persistence store for <see cref="ArtistInbox"/> entities.</param>
    public UpdateSubmissionStatusCommandHandler(CosmosContainer<Submission> container, IActorProvider actor, IPersistenceStore<Submission, Guid> submission, IPersistenceStore<ArtistInbox, Guid> artist)
    {
        _container = container.Container;
        _actor = actor;
        _submission = submission;
        _artist = artist;
    }

    /// <summary>
    /// Handles the <see cref="UpdateSubmissionStatusCommand"/> request asynchronously.
    /// </summary>
    /// <param name="request">The command specifying the submission to update, the artist, catalog, and the new status.</param>
    /// <param name="cancellationToken">A token to observe cancellation requests.</param>
    /// <returns>
    /// A <see cref="SubmissionStatusUpdatedResult"/> containing the identifiers of the affected entities
    /// and the old and new status values.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown if the submission or artist inbox cannot be retrieved, or if Cosmos DB operations fail.
    /// </exception>
    public async Task<SubmissionStatusUpdatedResult> Handle(UpdateSubmissionStatusCommand request, CancellationToken cancellationToken)
    {
        var curatorUserId = _actor.GetActorId();
        var correlationId = _actor.GetCorrelationId();

        var submission = await _submission.GetAsync(request.SubmissionId, new PartitionKeyBuilder().Add(curatorUserId.ToString()).Add(request.CatalogItemId.ToString()).Build(), cancellationToken);
        var artistInbox = await _artist.GetAsync(request.SubmissionId, new PartitionKey(request.ArtistId.ToString()), cancellationToken);

        if (!submission.IsSuccess || !artistInbox.IsSuccess)
        {
            var exception = submission.Exceptions.FirstOrDefault() ?? artistInbox.Exceptions.FirstOrDefault();
            throw exception!;
        }

        var s = submission.Result;
        var a = artistInbox.Result;
        if (s is null || a is null) return new SubmissionStatusUpdatedResult(request.SubmissionId, request.ArtistId, curatorUserId, request.NewStatus, request.NewStatus);

        var oldStatus = s.Status;
        if (oldStatus == request.NewStatus) return new SubmissionStatusUpdatedResult(request.SubmissionId, request.ArtistId, curatorUserId, oldStatus, request.NewStatus);

        s.UpdateStatus(request.NewStatus);
        a.UpdateStatus(request.NewStatus);

        var outboxEvent = OutboxEvent.CreateStatusChanged(s, oldStatus, correlationId);

        var batch = _container.CreateTransactionalBatch(s.GetPartitionKey()).ReplaceItem(s.Id.ToString(), s).CreateItem(outboxEvent);
        var artistUpdate = await _artist.UpdateAsync(a, cancellationToken);
        if (!artistUpdate.IsSuccess)
            throw artistUpdate.Exceptions.First();

        var resp = await batch.ExecuteAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode && resp.StatusCode != HttpStatusCode.Conflict)
            throw new BaseException($"Cosmos request failed when updating submission. Status: {resp.StatusCode}, ActivityId: {resp.ActivityId}", (int)resp.StatusCode, "Failed to update submission", "Please try again later or contact support if the issue persists", nameof(Submission));

        return new SubmissionStatusUpdatedResult(request.SubmissionId, request.ArtistId, curatorUserId, oldStatus, request.NewStatus);
    }
}