namespace SubmissionService.Models;

/// <summary>
/// Represents a request to create a new submission.
/// </summary>
/// <param name="CuratorUserId">The identifier of the curator creating the submission.</param>
/// <param name="CatalogItemId">The identifier of the catalog item to which the submission belongs.</param>
/// <param name="TrackId">The identifier of the track associated with the submission.</param>
public sealed record CreateSubmissionRequest(Guid CuratorUserId, Guid CatalogItemId, Guid TrackId);

/// <summary>
/// Represents a request to update the status of a submission.
/// </summary>
/// <param name="ArtistId">The identifier of the artist who owns the submission.</param>
/// <param name="NewStatus">The new status to assign to the submission.</param>
public sealed record UpdateSubmissionStatusRequest(Guid ArtistId, string NewStatus);
