using System.Net;
using Extensions.Extensions;
using MassTransit;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Components;
using Repository.Abstractions.Extensions;
using Spred.Bus.Contracts;
using SubmissionService.Models.Entities;

namespace SubmissionService.Components.Workers;

/// <summary>
/// Background worker that processes outbox events from Cosmos DB
/// and publishes them to the message bus using MassTransit.
/// </summary>
public sealed class OutboxWorker : BackgroundService
{
    private readonly Container _container;
    private readonly IPublishEndpoint _bus;
    private readonly ILogger<OutboxWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxWorker"/> class.
    /// </summary>
    /// <param name="container">The Cosmos DB container for outbox events.</param>
    /// <param name="bus">The MassTransit publish endpoint for sending messages.</param>
    /// <param name="loggerFactory">The logger factory used to create a worker logger.</param>
    public OutboxWorker(CosmosContainer<OutboxEvent> container, IPublishEndpoint bus,
        ILoggerFactory loggerFactory)
    {
        _container = container.Container;
        _bus = bus;
        _logger = loggerFactory.CreateLogger<OutboxWorker>();
    }

    /// <summary>
    /// Executes the main worker loop that queries pending outbox events,
    /// claims them, and publishes their payloads to the message bus.
    /// </summary>
    /// <param name="stoppingToken">A token to observe cancellation requests.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // better switch to Change Feed Processor
            var queryDefinition = new QueryDefinition(
                    "SELECT * FROM c WHERE c.type = @t AND c.state = @s AND NOT IS_DEFINED(c.lockedAt) ORDER BY c._ts")
                .WithParameter("@t", "Outbox")
                .WithParameter("@s", (int)OutboxEventState.Pending);

            using var iterator = _container.GetItemQueryIterator<OutboxEvent>(
                queryDefinition,
                requestOptions: new QueryRequestOptions { MaxItemCount = 50 });

            while (iterator.HasMoreResults && !stoppingToken.IsCancellationRequested)
            {
                var page = await iterator.ReadNextAsync(stoppingToken);

                foreach (var outboxEvent in page)
                {
                    if (!await TryClaimAsync(outboxEvent, stoppingToken))
                        continue;

                    try
                    {
                        if (outboxEvent.EventType == nameof(SubmissionCreated))
                        {
                            var payload = outboxEvent.Payload.ToObject<SubmissionCreated>();
                            await _bus.Publish(payload!, ctx =>
                            {
                                ctx.MessageId = outboxEvent.Id;
                                ctx.CorrelationId = outboxEvent.SubmissionId;
                            }, stoppingToken);
                        }
                        else if (outboxEvent.EventType == nameof(SubmissionStatusChanged))
                        {
                            var payload = outboxEvent.Payload.ToObject<SubmissionStatusChanged>();
                            await _bus.Publish(payload!, ctx =>
                            {
                                ctx.MessageId = outboxEvent.Id;
                                ctx.CorrelationId = outboxEvent.SubmissionId;
                            }, stoppingToken);
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported event type: {outboxEvent.EventType}");
                        }
                        
                        await MarkAsync(outboxEvent, OutboxEventState.Published, stoppingToken);
                    }
                    catch (System.Exception exception)
                    {
                        _logger.LogSpredError(
                            outboxEvent.EventType,
                            $"Failed to publish outbox event {outboxEvent.Id} for submission {outboxEvent.SubmissionId}",
                            exception);

                        await MarkAsync(outboxEvent, OutboxEventState.Failed, stoppingToken);
                    }
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    /// <summary>
    /// Attempts to claim an outbox event by marking it as locked.
    /// </summary>
    /// <param name="e">The outbox event to claim.</param>
    /// <param name="ct">A token to observe cancellation requests.</param>
    /// <returns><c>true</c> if the claim was successful; otherwise, <c>false</c>.</returns>
    private async Task<bool> TryClaimAsync(OutboxEvent e, CancellationToken ct)
    {
        try
        {
            await _container.PatchItemAsync<OutboxEvent>(
                e.Id.ToString(),
                e.GetPartitionKey(),
                new[]
                {
                    PatchOperation.Add("/lockedAt", DateTimeOffset.UtcNow),
                    PatchOperation.Add("/workerId", Environment.MachineName)
                },
                new PatchItemRequestOptions
                {
                    FilterPredicate =
                        $"FROM c WHERE c.state = {(int)OutboxEventState.Pending} AND NOT IS_DEFINED(c.lockedAt)"
                },
                ct);

            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed ||
                                         ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <summary>
    /// Marks the outbox event with a new state and updates its timestamp.
    /// </summary>
    /// <param name="e">The outbox event to update.</param>
    /// <param name="newState">The new state to assign to the event.</param>
    /// <param name="ct">A token to observe cancellation requests.</param>
    private async Task MarkAsync(OutboxEvent e, OutboxEventState newState, CancellationToken ct)
    {
        var ops = new List<PatchOperation>
        {
            PatchOperation.Replace("/State", (int)newState),
            PatchOperation.Remove("/lockedAt")
        };

        if (newState == OutboxEventState.Published)
            ops.Add(PatchOperation.Add("/PublishedAt", DateTimeOffset.UtcNow));
        else
            ops.Add(PatchOperation.Add("/FailedAt", DateTimeOffset.UtcNow));

        await _container.PatchItemAsync<OutboxEvent>(
            e.Id.ToString(),
            e.GetPartitionKey(),
            ops,
            cancellationToken: ct);
    }
}
