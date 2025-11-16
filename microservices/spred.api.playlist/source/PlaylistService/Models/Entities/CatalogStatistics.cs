using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;
using Spred.Bus.Contracts;

namespace PlaylistService.Models.Entities;

/// <summary>
/// Represents daily statistics for a cataloged playlist, including follower count and daily difference.
/// </summary>
public class CatalogStatistics : IBaseEntity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogStatistics"/> class with a new unique identifier.
    /// </summary>
    public CatalogStatistics()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogStatistics"/> class from a <see cref="StatInfo"/> object and playlist metadata ID.
    /// </summary>
    /// <param name="statInfo">The source statistical information.</param>
    /// <param name="metadataId">The ID of the related playlist metadata.</param>
    public CatalogStatistics(StatInfo statInfo, Guid metadataId) : this()
    {
        MetadataId = metadataId;
        Date = statInfo.Timestamp;
        Followers = statInfo.Value;
        FollowersDailyDiff = statInfo.DailyDiff;
    }

    /// <summary>
    /// The ID of the related playlist metadata. Used as the partition key in Cosmos DB.
    /// </summary>
    [PartitionKey]
    public Guid MetadataId { get; init; }

    /// <summary>
    /// The date when the statistic was recorded.
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// The total number of followers recorded on the specified date.
    /// </summary>
    public uint Followers { get; init; }

    /// <summary>
    /// The difference in follower count compared to the previous day.
    /// </summary>
    public int FollowersDailyDiff { get; set; }

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public string? ETag { get; }
    
    /// <inheritdoc />
    public long Timestamp { get; }
}