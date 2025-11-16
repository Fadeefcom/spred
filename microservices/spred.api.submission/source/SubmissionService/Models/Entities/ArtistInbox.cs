using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;

namespace SubmissionService.Models.Entities;

/// <summary>
/// Represents an inbox entry for an artist, used to track submissions
/// associated with a curator, catalog item, and track.
/// </summary>
public class ArtistInbox : IBaseEntity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArtistInbox"/> class
    /// with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the inbox entry.</param>
    public ArtistInbox(Guid id)
    {
        Id = id;
        Type = "ArtistInbox";
    }

    /// <summary>
    /// Gets the unique identifier of the inbox entry.
    /// </summary>
    public Guid Id { get; private set; }

    /// <inheritdoc/>
    public string? ETag { get; private set; }

    /// <inheritdoc/>
    public long Timestamp { get; private set; }

    /// <summary>
    /// Gets the type label used to identify this entity in storage.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Gets the identifier of the artist associated with this inbox entry.
    /// </summary>
    [PartitionKey]
    public Guid ArtistId { get; init; }

    /// <summary>
    /// Gets the identifier of the curator associated with the inbox entry.
    /// </summary>
    public Guid CuratorUserId { get; init; }

    /// <summary>
    /// Gets the identifier of the catalog item linked to the inbox entry.
    /// </summary>
    public Guid CatalogItemId { get; init; }

    /// <summary>
    /// Gets the identifier of the track included in the inbox entry.
    /// </summary>
    public Guid TrackId { get; init; }

    /// <summary>
    /// Gets the current status of the submission in the inbox.
    /// </summary>
    public SubmissionStatus Status { get; private set; }

    /// <summary>
    /// Gets the date and time when the inbox entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the date and time when the inbox entry was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Updates the status of the inbox entry and refreshes the update timestamp.
    /// </summary>
    /// <param name="newStatus">The new submission status to assign.</param>
    public void UpdateStatus(SubmissionStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
