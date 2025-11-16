using SubmissionService.Models.Entities;

namespace SubmissionService.Models.Models;

/// <summary>
/// Represents a data transfer object for a submission.
/// </summary>
public sealed record SubmissionDto
{
    /// <summary>
    /// Gets the unique identifier of the submission.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the identifier of the artist associated with the submission.
    /// </summary>
    public Guid ArtistId { get; init; }

    /// <summary>
    /// Gets the identifier of the curator associated with the submission.
    /// </summary>
    public Guid CuratorUserId { get; init; }

    /// <summary>
    /// Gets the identifier of the catalog item linked to the submission.
    /// </summary>
    public Guid CatalogItemId { get; init; }

    /// <summary>
    /// Gets the identifier of the track included in the submission.
    /// </summary>
    public Guid TrackId { get; init; }

    /// <summary>
    /// Gets the current status of the submission.
    /// </summary>
    public SubmissionStatus Status { get; init; }

    /// <summary>
    /// Gets the date and time when the submission was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}