using Extensions.Extensions;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Components;
using Repository.Abstractions.Interfaces;
using StackExchange.Redis;
using SubscriptionService.Abstractions;
using SubscriptionService.Models;
using SubscriptionService.Models.Entities;

namespace SubscriptionService.Components;

/// <inheritdoc />
public class SubscriptionStateStore : ISubscriptionStateStore
{
    private readonly Container _container;
    private readonly IPersistenceStore<UserSubscriptionStatus, Guid> _store;
    private readonly IPersistenceStore<SubscriptionSnapshot, Guid> _snapshotStore;
    private readonly IDatabase _redis;
    private readonly ILogger<SubscriptionStateStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionStateStore"/> class with the specified persistence store.
    /// </summary>
    /// <param name="store">
    /// The persistence store responsible for managing <see cref="UserSubscriptionStatus"/> entities in Cosmos DB. 
    /// This store is used to retrieve and persist subscription status information for individual users.
    /// </param>
    /// <param name="snapshotStore">Snapshot store.</param>
    /// <param name="container"></param>
    /// <param name="connectionMultiplexer">Redis connection.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public SubscriptionStateStore(IPersistenceStore<UserSubscriptionStatus, Guid> store,
        IPersistenceStore<SubscriptionSnapshot, Guid> snapshotStore, CosmosContainer<UserSubscriptionStatus> container,
        IConnectionMultiplexer connectionMultiplexer, ILoggerFactory loggerFactory)
    {
        _container = container.Container;
        _store = store;
        _redis = connectionMultiplexer.GetDatabase();
        _logger = loggerFactory.CreateLogger<SubscriptionStateStore>();
        _snapshotStore = snapshotStore;
    }
    
    /// <inheritdoc />
    public async Task<bool?> GetStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _store.GetAsync(
            predicate: _ => true,
            sortSelector: x => x.Timestamp,
            partitionKey: new PartitionKey(userId.ToString()),
            offset: 0,
            limit: 1,
            descending: true,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
            return null;

        var record = result.Result?.FirstOrDefault();
        if (record == null)
            return null;

        if (record.CurrentPeriodEnd.HasValue && record.CurrentPeriodEnd.Value < DateTime.UtcNow)
            return false;

        return record.IsActive;
    }
    
    /// <inheritdoc />
    public async Task<Guid?> SetStatusAsync(Guid userId, string paymentId, bool isActive, string subscriptionId, string? logicalState, DateTime? periodStart,
        DateTime? periodEnd, CancellationToken cancellationToken)
    {
        var entity = new UserSubscriptionStatus
        {
            UserId = userId,
            IsActive = isActive,
            CurrentPeriodStart = periodStart,
            CurrentPeriodEnd = periodEnd,
            PaymentId = paymentId,
            SubscriptionId = subscriptionId,
            LogicalState = logicalState ?? string.Empty
        };

        var result = await _store.StoreAsync(entity, cancellationToken);
        
        if (!result.IsSuccess)
        {
            foreach (var exception in result.Exceptions)
            {
                _logger.LogSpredError("SetStatusAsync", $"Failed to store UserSubscriptionStatus for user {userId}, {isActive}", exception);
            }
        }

        if (result is { IsSuccess: true, Result: true })
        {
            if (periodEnd.HasValue)
            {
                var ttl = periodEnd.Value - DateTime.UtcNow;
                if (ttl > TimeSpan.Zero)
                {
                    await _redis.StringSetAsync($"subscription:{userId}", isActive, ttl);
                    return entity.Id;
                }
            }

            await _redis.StringSetAsync($"subscription:{userId}", false);
            return entity.Id;
        }

        _logger.LogSpredWarning("SetStatusAsync", $"Failed to store UserSubscriptionStatus for user {userId}, {isActive}");
        return null;
    }

    /// <inheritdoc />
    public async Task<UserSubscriptionStatus?> GetDetailsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await _store.GetAsync(
            predicate: _ => true,
            sortSelector: x => x.Timestamp,
            partitionKey: new PartitionKey(userId.ToString()),
            offset: 0,
            limit: 1,
            descending: true,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess || (result.Exceptions.Count) > 0)
        {
            if (result.Exceptions is { Count: > 0 })
            {
                foreach (var ex in result.Exceptions)
                {
                    _logger.LogSpredError(
                        "SubscriptionStateStore.GetDetailsAsyncFailed",
                        $"Failed to read subscription details for user {userId}",
                        ex);
                }
            }
            else
            {
                _logger.LogSpredWarning(
                    "SubscriptionStateStore.GetDetailsAsyncUnknownError",
                    $"Failed to read subscription details for user {userId} (no exception provided)");
            }
            return null;
        }

        return result.Result?.Any() == true ? result.Result.First() : null;
    }

    /// <inheritdoc />
    public async Task<Guid?> SaveSnapshotAsync(Guid userId, Guid statusId, string kind, string id, string rawJson,
        CancellationToken cancellationToken)
    {
        var snapshot = new SubscriptionSnapshot()
        {
            UserId = userId,
            StatusId = statusId,
            Kind = kind,
            ExternalId = id,
            RawJson = rawJson
        };
        
        var result = await _snapshotStore.StoreAsync(snapshot, cancellationToken);
        
        var safeContext = $"UserId={snapshot.UserId}, StatusId={snapshot.StatusId}, Kind={snapshot.Kind}, ExternalId={snapshot.ExternalId}";
        
        if (!result.IsSuccess)
        {
            foreach (var ex in result.Exceptions)
            {
                _logger.LogSpredError(
                    "SubscriptionStateStore.SaveSnapshotAsyncFailed",
                    $"Failed to save snapshot. {safeContext}",
                    ex);
            }

            return null;
        }
        else
        {
            _logger.LogSpredInformation(
                "SubscriptionStateStore.SaveSnapshotAsyncOk",
                $"Snapshot {snapshot.Id} saved. {safeContext}");
            return snapshot.Id;
        }
    }
    
    /// <inheritdoc />
    public async Task<AtomicSaveResult> SaveAtomicAsync(
        Guid userId,
        UserSubscriptionStatus status,
        string kind, string externalId, string rawJson,
        CancellationToken cancellationToken = default)
    {
        var pk = new PartitionKey(userId.ToString());
        var batch = _container.CreateTransactionalBatch(pk);
        
        batch = batch.UpsertItem(status);
        
        var snap = new SubscriptionSnapshot
        {
            UserId = userId,
            StatusId = status.Id,
            Kind = kind,
            ExternalId = externalId,
            RawJson = rawJson
        };

        batch = batch.UpsertItem(snap);

        var resp = await batch.ExecuteAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode)
            return new AtomicSaveResult(false, false, null, null, resp.StatusCode, resp.ErrorMessage);

        var r1 = resp.GetOperationResultAtIndex<UserSubscriptionStatus>(0);
        var r2 = resp.GetOperationResultAtIndex<SubscriptionSnapshot>(1);

        var statusSaved = r1.StatusCode is System.Net.HttpStatusCode.Created or System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.NoContent;
        var snapshotSaved = r2.StatusCode is System.Net.HttpStatusCode.Created or System.Net.HttpStatusCode.OK or System.Net.HttpStatusCode.NoContent;

        return new AtomicSaveResult(statusSaved, snapshotSaved, r1.ETag, r2.ETag, resp.StatusCode, null);
    }
}