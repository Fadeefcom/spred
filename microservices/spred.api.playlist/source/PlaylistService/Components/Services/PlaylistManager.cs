using System.Linq.Expressions;
using Exception;
using Exception.Exceptions;
using Extensions.Extensions;
using Extensions.Utilities;
using Microsoft.Azure.Cosmos;
using PlaylistService.Abstractions;
using PlaylistService.Models;
using PlaylistService.Models.Entities;
using Repository.Abstractions.Interfaces;

namespace PlaylistService.Components.Services;

/// <summary>
/// Manager for handling playlist operations.
/// </summary>
public class ManagerPlaylist : IManager
{
    private readonly IPersistenceStore<CatalogMetadata, Guid> _playlistStore;
    private readonly IPersistenceStore<CatalogStatistics, Guid> _statisticsStore;
    private readonly ILogger<ManagerPlaylist> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagerPlaylist"/> class.
    /// </summary>
    /// <param name="playlistStore">The persistence store for playlist metadata.</param>
    /// <param name="statisticsStore">The persistence store for playlist statistics.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public ManagerPlaylist(IPersistenceStore<CatalogMetadata, Guid> playlistStore, IPersistenceStore<CatalogStatistics, Guid> statisticsStore,
        ILoggerFactory loggerFactory)
    {
        _playlistStore = playlistStore;
        _logger = loggerFactory.CreateLogger<ManagerPlaylist>();
        _statisticsStore = statisticsStore;
    }

    /// <inheritdoc/>
    public async Task<bool> AddAsync(CatalogMetadata entity, CancellationToken cancellationToken)
    {
        var result = await _playlistStore.StoreAsync(entity, cancellationToken);
            if (result.IsSuccess)
                return true;

        _logger.LogSpredWarning("SavePlaylist", "Failed to add playlist", result.Exceptions.First());
        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid playlistId, Guid spredUserId, CancellationToken cancellationToken, string bucket = "00")
    {
        Expression<Func<CatalogMetadata, bool>> expression = x => x.Id == playlistId;
        Expression<Func<CatalogMetadata, long>> sortSelector = x => x.Timestamp;

        var result = await _playlistStore.GetAsync(expression, sortSelector, 
            new PartitionKeyBuilder().Add(spredUserId.ToString()).Add(bucket).Build(), 
            0, 1, false, cancellationToken);
        if (result.IsSuccess)
        {
            if (result.Result == null)
                return true;

            var subResult = await _playlistStore.DeleteAsync(result.Result.First(), cancellationToken);
            return subResult is { IsSuccess: true, Result: true };
        }

        _logger.LogSpredWarning("DeletePlaylist", "Failed to delete playlist", result.Exceptions.First());
        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(CatalogMetadata entity, CancellationToken cancellationToken)
    {
        var result = await _playlistStore.UpdateAsync(entity, cancellationToken);

        if (!result.IsSuccess)
            _logger.LogSpredWarning("UpdatePlaylist", "Failed to update playlist", result.Exceptions.First());

        return result is { IsSuccess: true, Result: true };
    }

    /// <inheritdoc/>
    public async Task<CatalogMetadata?> FindByIdAsync(Guid id, Guid spredUserId, CancellationToken cancellationToken, string bucket = "00")
    {
        var result = await _playlistStore.GetAsync(id, 
            new PartitionKeyBuilder().Add(spredUserId.ToString()).Add(bucket).Build(), cancellationToken, true);

        return !result.IsSuccess ? throw result.Exceptions.First() : result.Result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<CatalogMetadata>> GetAsync(Dictionary<string, string> queryParams, string type, Guid spredUserId,
        CancellationToken cancellationToken, string bucket = "00")
    {
        Expression<Func<CatalogMetadata, bool>> expression = x => x.Type == type && x.IsDeleted == false;
        Expression<Func<CatalogMetadata, long>> sortSelector = x => x.Timestamp;

        var limit = queryParams.GetLimit();
        var offset = queryParams.GetOffset();

        var result = await _playlistStore.GetAsync(expression, sortSelector, 
            new PartitionKeyBuilder().Add(spredUserId.ToString()).Add(bucket).Build(), 
            offset, limit, false, cancellationToken);

        if (result.IsSuccess)
            return result.Result ?? [];

        _logger.LogSpredError($"Failed to get track metadata items for user with ID: {spredUserId}",
            result.Exceptions.First());
        throw new BaseException(message: result.Exceptions.First().Message,
            status: (int)ErrorCode.NotFound, title: "Get failed", type: $"{typeof(ManagerPlaylist)} failed",
            details: "Get items operation aborted, try again later.");
    }

    /// <inheritdoc/>
    public async Task<Guid?> ExistsByPrimaryIdAsync(PrimaryId primaryId, Guid spredUserId, CancellationToken cancellationToken)
    {
        Expression<Func<CatalogMetadata, bool>> predicate = x => x.PrimaryId == primaryId.ToString() && x.IsDeleted == false;
        Expression<Func<CatalogMetadata, long>> sortSelector = x => x.Timestamp;
        
        if (spredUserId.Equals(Guid.Empty))
        {
            var playlistResult = await _playlistStore.GetAsync(predicate, sortSelector, new PartitionKey(Guid.Empty.ToString()), 
                0, 1, false, cancellationToken, true);
            
            return playlistResult.Result?.FirstOrDefault()?.Id;
        }

        var partitionKey = new PartitionKeyBuilder().Add(spredUserId.ToString()).Add("00").Build();

        var result = await _playlistStore.GetAsync(predicate, sortSelector, partitionKey,
            0, 1, false, cancellationToken);

        if (result.IsSuccess)
            return result.Result?.FirstOrDefault()?.Id;

        _logger.LogSpredError("IfExistsByPrimaryIdTrackLookup",$"Failed to check if track metadata item exists with primary ID: {primaryId}",
            result.Exceptions.First());
        return null;
    }

    /// <inheritdoc/>
    public async Task<int> GetStatisticDifference(Guid playlistId, CancellationToken cancellationToken)
    {
        var dateThreshold = DateTime.UtcNow.Date.AddDays(-30);
        var today = DateTime.UtcNow.Date.AddDays(1);
        Expression<Func<CatalogStatistics, bool>> expression = x => x.Date >= dateThreshold && x.Date < today;
        Expression<Func<CatalogStatistics, long>> sortSelector = x => x.Timestamp;
        
        var result = await _statisticsStore.GetAsync(expression, sortSelector, 
            new PartitionKey(playlistId.ToString()), 0, 31, false, cancellationToken, true);

        if (result.IsSuccess)
        {
            var firstValue = result.Result?.FirstOrDefault()?.Followers ?? 0;
            var lastValue = result.Result?.LastOrDefault()?.Followers ?? 0;

            return (int)firstValue - (int)lastValue;
        }

        return -1;
    }

    /// <inheritdoc/>
    public async Task<List<CatalogMetadata>> GetPlaylistsByIdsAsync(Dictionary<Guid, List<Guid>> ownerMetadataIds, string type, 
        CancellationToken cancellationToken)
    {
        Expression<Func<CatalogMetadata, bool>> expression = x => x.Type == type;
        Expression<Func<CatalogMetadata, long>> sortSelector = x => x.Timestamp;
        
        var keys = ownerMetadataIds
            .SelectMany(kvp => kvp.Value.Select(id => (id.ToString(), BuildPartitionKey(id, kvp.Key))))
            .ToList();

        var result = await _playlistStore.ReadManyItemsAsync(expression, sortSelector, keys, true, cancellationToken);
        return result;
    }

    private static PartitionKey BuildPartitionKey(Guid id, Guid partitionKey)
    {
        var builder = new PartitionKeyBuilder().Add(partitionKey.ToString());
        if (partitionKey == Guid.Empty)
        {
            builder.Add(GuidShortener.GenerateBucketFromGuid(id));
        }
        else
            builder.Add("00");
        
        return builder.Build();
    }
}
