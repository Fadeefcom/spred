using System.Globalization;
using System.Linq.Expressions;
using Exception;
using Exception.Exceptions;
using Extensions.Extensions;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Models;
using TrackService.Abstractions;
using TrackService.Models.Entities;

namespace TrackService.Components.Services;

/// <summary>
/// Repository for managing track metadata.
/// </summary>
public sealed class TrackManager : ITrackManager
{
    private readonly IPersistenceStore<TrackMetadata, Guid> _persistenceStore;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackManager"/> class.
    /// </summary>
    /// <param name="persistenceStore">The persistence store.</param>
    /// <param name="factory">The logger factory.</param>
    public TrackManager(IPersistenceStore<TrackMetadata, Guid> persistenceStore, ILoggerFactory factory)
    {
        _persistenceStore = persistenceStore;
        _logger = factory.CreateLogger<TrackManager>();
    }

    /// <inheritdoc/>
    public async Task<Guid> AddAsync(TrackMetadata userTrack, Guid spredUserId,
        CancellationToken cancellationToken = default)
    {
        var result = await _persistenceStore.StoreAsync(userTrack, cancellationToken);
        if (result.IsSuccess)
            return userTrack.Id;

        _logger.LogSpredError("StoreTrackMetadata",$"Failed to add track metadata item with ID: {userTrack.Id}", result.Exceptions.First());
        throw new BaseException(message: result.Exceptions.First().Message, status: (int)ErrorCode.Conflict,
            title: "Create failed", type: $"{typeof(TrackManager)} failed",
            details: "Create item operation aborted, try again later.");
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid id, Guid spredUserId, CancellationToken cancellationToken, string bucket)
    {
        var partitionKey  = new PartitionKeyBuilder().Add(spredUserId.ToString()).Add(bucket).Build();
        var result = await _persistenceStore.GetAsync(id, partitionKey, cancellationToken);
        if (result.IsSuccess && result.Result?.SpredUserId == spredUserId)
        {
            if (result.Result.IsDeleted)
                return true;
            
            result.Result.Delete();
            var tempPersistenceResult = await _persistenceStore.UpdateAsync(result.Result, cancellationToken);
            return tempPersistenceResult.IsSuccess;
        }

        _logger.LogSpredError("DeleteAsyncTrackMetadata",$"Failed to delete track metadata item with ID: {id}", result.Exceptions.First());
        throw new BaseException(message: result.Exceptions.First().Message, status: (int)ErrorCode.Conflict,
            title: "Delete failed", type: $"{typeof(TrackManager)} failed",
            details: "Delete item operation aborted, try again later.");
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TrackMetadata>> GetAsync(Dictionary<string, string> queryParams, Guid spredUserId,
        CancellationToken cancellationToken, string bucket)
    {
        var partitionKey  = new PartitionKeyBuilder().Add(spredUserId.ToString()).Add(bucket).Build();
        Expression<Func<TrackMetadata, bool>> predicate = x => x.IsDeleted == false;
        Expression<Func<TrackMetadata, long>> sortSelector = x => x.Timestamp;

        var limit = queryParams.GetLimit();
        var offset = queryParams.GetOffset();
        predicate = queryParams.PredicateBuilder(predicate);

        var result = await _persistenceStore.GetAsync(predicate, sortSelector, partitionKey, offset, limit, 
            true, cancellationToken);

        if (result.IsSuccess)
            return result.Result ?? [];

        _logger.LogSpredError("GetAsyncTrackMetadata",$"Failed to get track metadata items for user with ID: {spredUserId}",
            result.Exceptions.First());
        return [];
    }

    /// <inheritdoc/>
    public async Task<TrackMetadata?> GetByIdAsync(Guid id, Guid spredUserId,
        CancellationToken cancellationToken, string bucket)
    {
        var partitionKey  = new PartitionKeyBuilder().Add(spredUserId.ToString()).Add(bucket).Build();
        var result = await _persistenceStore.GetAsync(id, partitionKey, cancellationToken, true);
        if(result.IsSuccess)
            return result.Result;

        _logger.LogSpredWarning("GetByIdAsyncTrackMetadata", $"Failed to get track metadata item with ID: {id}");
        return null;
    }

    /// <inheritdoc/>
    public async Task<int> GetTotalAsync(Dictionary<string, string> queryParams, Guid spredUserId,
        CancellationToken cancellationToken, string bucket)
    {
        var partitionKey  = new PartitionKeyBuilder().Add(spredUserId.ToString()).Add(bucket).Build();
        Expression<Func<TrackMetadata, bool>> predicate = x => x.IsDeleted == false;
        predicate = queryParams.PredicateBuilder(predicate);

        var resultCount = await _persistenceStore.CountAsync(predicate, partitionKey, cancellationToken);

        if (resultCount.IsSuccess)
            return resultCount.Result;

        _logger.LogSpredError("GetTotalAsyncTrackMetadata",$"Failed to get total count of track metadata items for user with ID: {spredUserId}",
            resultCount.Exceptions.First());
        return 0;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(TrackMetadata entity, CancellationToken cancellationToken)
    {
        var result = await _persistenceStore.UpdateAsync(entity, cancellationToken);
        if (result.IsSuccess)
            return true;

        _logger.LogSpredError("UpdateAsyncTrackMetadata",$"Failed to update track metadata item with ID: {entity.Id}", result.Exceptions.First());
        throw result.Exceptions.First();
    }

    /// <inheritdoc/>
    public async Task<Guid?> IfExistsByPrimaryId(string primaryId, Guid spredUserId, CancellationToken cancellationToken)
    {
        Expression<Func<TrackMetadata, bool>> predicate = x => x.PrimaryId == primaryId && x.IsDeleted == false;
        Expression<Func<TrackMetadata, long>> sortSelector = x => x.Timestamp;
        
        if (spredUserId.Equals(Guid.Empty))
        {
            List<Task<PersistenceResult<IEnumerable<TrackMetadata>>>> tasks = [];
            List<PartitionKey> partitionKeys = [];
            for (int i = 0; i < 10; i++)
                partitionKeys.Add(new PartitionKeyBuilder().Add(spredUserId.ToString())
                    .Add(i.ToString("D2", CultureInfo.InvariantCulture)).Build());

            foreach (var key in partitionKeys)
            {
                tasks.Add(_persistenceStore.GetAsync(predicate, sortSelector, key,
                    0, 1, false, cancellationToken, true));
            }
            
            await Task.WhenAll(tasks);
            return tasks.Select(t => t.Result.Result?.FirstOrDefault()?.Id)
                .Where(t => t != null).ToList().FirstOrDefault();
        }

        var partitionKey = new PartitionKeyBuilder().Add(spredUserId.ToString()).Add("00").Build();

        var result = await _persistenceStore.GetAsync(predicate, sortSelector, partitionKey,
            0, 1, false, cancellationToken);

        if (result.IsSuccess)
            return result.Result?.FirstOrDefault()?.Id;

        _logger.LogSpredError("IfExistsByPrimaryIdTrackLookup",$"Failed to check if track metadata item exists with primary ID: {primaryId}",
            result.Exceptions.First());
        return null;
    }
}