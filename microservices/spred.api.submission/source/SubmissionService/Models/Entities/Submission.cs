using Repository.Abstractions.Extensions;
using Repository.Abstractions.Interfaces.BaseEntity;

namespace SubmissionService.Models.Entities;

/// <summary>
/// Represents a submission entity stored in the database,
/// containing information about the artist, curator, catalog item,
/// track, and submission status.
/// </summary>
public sealed class Submission : IBaseEntity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Submission"/> class
    /// with default values for identifier, type, creation date,
    /// update date, and status.
    /// </summary>
    public Submission()
    {
        Id = Guid.NewGuid();
        Type = "Submission";
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        Status = SubmissionStatus.Created;
    }

    /// <summary>
    /// Gets the unique identifier of the submission.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the type label used to identify this entity in storage.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Gets the identifier of the artist associated with the submission.
    /// </summary>
    public Guid ArtistId { get; init; }

    /// <summary>
    /// Gets the identifier of the curator associated with the submission.
    /// </summary>
    [PartitionKey(0)]
    public Guid CuratorUserId { get; init; }

    /// <summary>
    /// Gets the identifier of the catalog item linked to the submission.
    /// </summary>
    [PartitionKey(1)]
    public Guid CatalogItemId { get; init; }

    /// <summary>
    /// Gets the identifier of the track included in the submission.
    /// </summary>
    public Guid TrackId { get; init; }

    /// <summary>
    /// Gets the current status of the submission.
    /// </summary>
    public SubmissionStatus Status { get; private set; }

    /// <summary>
    /// Gets the date and time when the submission was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the date and time when the submission was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <inheritdoc/>
    public string? ETag { get; private set; }

    /// <inheritdoc/>
    public long Timestamp { get; private set; }

    /// <summary>
    /// Updates the status of the submission and refreshes the update timestamp.
    /// </summary>
    /// <param name="status">The new status to assign to the submission.</param>
    public void UpdateStatus(SubmissionStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Represents the possible statuses of a submission.
/// </summary>
public enum SubmissionStatus
{
    /// <summary>
    /// The submission has been created but not yet processed.
    /// </summary>
    Created,

    /// <summary>
    /// The submission has been approved.
    /// </summary>
    Approved,

    /// <summary>
    /// The submission has been rejected.
    /// </summary>
    Rejected,

    /// <summary>
    /// The submission has been deleted.
    /// </summary>
    Deleted
}

