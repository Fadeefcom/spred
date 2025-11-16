using Exception.Exceptions;
using MediatR;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Components;
using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces;
using Spred.Bus.Abstractions;
using SubmissionService.Abstractions;
using SubmissionService.Models.Commands;
using SubmissionService.Models.Entities;

namespace SubmissionService.Components.Handlers.CommandHandlers;

/// <summary>
/// Handles <see cref="CreateSubmissionCommand"/> requests by creating a new submission,
/// storing an artist inbox entry, and writing an outbox event for downstream processing.
/// </summary>
/// <remarks>
/// This command handler validates the track and catalog references via external services,
/// persists the submission and its associated artist inbox entry transactionally in Cosmos DB,
/// and returns a result with identifiers and a snapshot of the created submission.
/// </remarks>
public sealed class CreateSubmissionCommandHandler : IRequestHandler<CreateSubmissionCommand, SubmissionCreatedResult>
{
    private readonly Container _container;
    private readonly IActorProvider _actor;
    private readonly IPersistenceStore<Submission, Guid> _submission;
    private readonly IPersistenceStore<ArtistInbox, Guid> _artist;
    private readonly ITrackService _trackService;
    private readonly ICatalogService _catalogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSubmissionCommandHandler"/> class.
    /// </summary>
    /// <param name="container">The Cosmos DB container used for transactional batch operations.</param>
    /// <param name="actor">The provider for retrieving the current actor identity and correlation metadata.</param>
    /// <param name="submission">The persistence store for <see cref="Submission"/> entities.</param>
    /// <param name="artist">The persistence store for <see cref="ArtistInbox"/> entities.</param>
    /// <param name="trackService">The external track service client used to validate track existence.</param>
    /// <param name="catalogService">The external catalog service client used to validate catalog existence.</param>
    public CreateSubmissionCommandHandler(CosmosContainer<Submission> container, IActorProvider actor,
        IPersistenceStore<Submission, Guid> submission, IPersistenceStore<ArtistInbox, Guid> artist,
        ITrackService trackService, ICatalogService catalogService)
    {
        _container = container.Container;
        _actor = actor;
        _submission = submission;
        _artist = artist;
        _trackService = trackService;
        _catalogService = catalogService;
    }

    /// <summary>
    /// Handles the <see cref="CreateSubmissionCommand"/> request asynchronously.
    /// </summary>
    /// <param name="request">The command specifying the submission details (track, catalog, curator).</param>
    /// <param name="cancellationToken">A token to observe cancellation requests.</param>
    /// <returns>
    /// A <see cref="SubmissionCreatedResult"/> containing the submission identifier,
    /// the involved users, and a snapshot of the created submission state.
    /// </returns>
    /// <exception cref="BaseException">
    /// Thrown when track or catalog validation fails, or when Cosmos DB operations fail.
    /// </exception>
    public async Task<SubmissionCreatedResult> Handle(CreateSubmissionCommand request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var artistId = _actor.GetActorId();
        var correlationId = _actor.GetCorrelationId();

        var trackByIdAsync = await _trackService.GetTrackByIdAsync(request.Request.TrackId.ToString(), artistId.ToString(),
            cancellationToken);
        if (!trackByIdAsync.IsSuccessful)
            throw new BaseException(
                $"Failed to fetch track {request.Request.TrackId} for artist {artistId}. Error: {trackByIdAsync.Error}", 404,
                "Track not found",
                "The specified track could not be retrieved. Please verify the track ID or try again later.",
                nameof(Submission));

        var playlistByIdAsync = await _catalogService.GetPlaylistByIdAsync(request.Request.CatalogItemId.ToString(),
            request.Request.CuratorUserId.ToString(), cancellationToken);
        if (!playlistByIdAsync.IsSuccessful)
            throw new BaseException(
                $"Failed to fetch catalog item {request.Request.CatalogItemId} for curator {request.Request.CuratorUserId}. Error: {playlistByIdAsync.Error}",
                404, "Catalog item not found",
                "The specified catalog item could not be retrieved. Please verify the catalog ID or try again later.",
                nameof(Submission));

        var submission = new Submission
        {
            ArtistId = artistId, CuratorUserId = request.Request.CuratorUserId,
            CatalogItemId = request.Request.CatalogItemId, TrackId = request.Request.TrackId, CreatedAt = now
        };
        var artistInbox = new ArtistInbox(submission.Id)
        {
            ArtistId = artistId, CuratorUserId = request.Request.CuratorUserId,
            CatalogItemId = request.Request.CatalogItemId, TrackId = request.Request.TrackId, CreatedAt = now
        };

        var partitionKey = submission.GetPartitionKey();
        var outboxEvent = OutboxEvent.CreateSubmissionCreated(submission, correlationId);

        var artistResult = await _artist.StoreAsync(artistInbox, cancellationToken);
        if (!artistResult.IsSuccess)
            throw artistResult.Exceptions.First();

        var batch = _container.CreateTransactionalBatch(partitionKey).CreateItem(submission).CreateItem(outboxEvent);
        var resp = await batch.ExecuteAsync(cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            if (artistResult.IsSuccess) await _artist.DeleteAsync(artistInbox, cancellationToken);
            throw new BaseException(
                $"Cosmos request failed when creating submission. Status: {resp.StatusCode}, ActivityId: {resp.ActivityId}",
                (int)resp.StatusCode, "Failed to create submission",
                "Please try again later or contact support if the issue persists", nameof(Submission));
        }
        
        var trackTitle = trackByIdAsync.Content.GetProperty("title").GetString();
        var playlistName = playlistByIdAsync.Content.GetProperty("name").GetString();

        var afterSnapshot = new
        {
            status = submission.Status, trackId = submission.TrackId, catalogItemId = submission.CatalogItemId,
            createdAt = submission.CreatedAt,
            catalogName = playlistName,
            trackName = trackTitle
        };
        return new SubmissionCreatedResult(submission.Id, artistId, request.Request.CuratorUserId, afterSnapshot);
    }
}