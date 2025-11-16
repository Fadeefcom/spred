using ActivityService.Abstractions;
using ActivityService.Models;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Interfaces;

namespace ActivityService.Components.Services;

/// <summary>
/// Service responsible for managing activity feeds.
/// </summary>
public sealed class ActivityFeedService
{
    /// <summary>
    /// Represents the persistence store instance used to interact with the underlying data storage for
    /// performing CRUD operations related to <see cref="ActivityEntity"/>.
    /// </summary>
    /// <remarks>
    /// The persistence store is designed to manage and retrieve records of type <see cref="ActivityEntity"/>,
    /// identified by a unique <see cref="Guid"/> key. It facilitates data access within the
    /// activity feed service, including operations such as retrieving, adding, and updating activities.
    /// </remarks>
    private readonly IPersistenceStore<ActivityEntity, Guid> _persistenceStore;

    /// <summary>
    /// An instance of <see cref="IActivityMessageFormatter"/> responsible for formatting activity messages.
    /// Provides a mechanism to generate readable representation of activity data for use in user-facing outputs.
    /// </summary>
    private readonly IActivityMessageFormatter _formatter;

    /// <summary>
    /// Provides services for retrieving and formatting activity feeds for users.
    /// </summary>
    /// <remarks>
    /// This service is responsible for interacting with a persistence store to retrieve activity entities
    /// and format them using an activity message formatter. It supports paging of activity feed results
    /// for efficient data retrieval. The service is designed to handle asynchronous operations.
    /// </remarks>
    public ActivityFeedService(IPersistenceStore<ActivityEntity, Guid> persistenceStore, IActivityMessageFormatter formatter)
    {
        _persistenceStore = persistenceStore;
        _formatter = formatter;
    }

    /// <summary>
    /// Retrieves a list of user activity feed items based on the provided offset and limit.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose feed is to be retrieved.</param>
    /// <param name="offset">The number of items to skip from the start of the feed.</param>
    /// <param name="limit">The maximum number of items to retrieve.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of formatted activity feed items.</returns>
    public async Task<IReadOnlyList<object>> GetUserFeedAsync(Guid userId, int offset, int limit, CancellationToken cancellationToken)
    {
        var activities = await _persistenceStore.GetAsync(_ => true, e => e.Timestamp,
            new PartitionKey(userId.ToString()), offset, limit, false, cancellationToken);

        return activities.Result?
            .Select(a => new ActivityFeedItem
            {
                Id = a.Id,
                CreatedAt = a.CreatedAt,
                Verb = a.Verb,
                ObjectType = a.ObjectType,
                ObjectId = a.ObjectId,
                Message = _formatter.Format(a)
            })
            .ToList() ?? [];
    }
}