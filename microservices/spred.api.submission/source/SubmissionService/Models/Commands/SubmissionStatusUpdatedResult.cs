namespace SubmissionService.Models.Commands;

/// <summary>
/// Result returned after updating the status of an existing submission.
/// Provides identifiers of the submission and related users, along with
/// both the previous and the newly assigned status values.
/// </summary>
/// <param name="SubmissionId">
/// The unique identifier of the submission whose status was updated.
/// </param>
/// <param name="ArtistUserId">
/// The identifier of the artist who owns the submission.
/// </param>
/// <param name="CuratorUserId">
/// The identifier of the curator associated with the submission's catalog item.
/// </param>
/// <param name="OldStatus">
/// The status of the submission before the update.
/// </param>
/// <param name="NewStatus">
/// The status of the submission after the update.
/// </param>
public sealed record SubmissionStatusUpdatedResult(
    Guid SubmissionId,
    Guid ArtistUserId,
    Guid CuratorUserId,
    Entities.SubmissionStatus OldStatus,
    Entities.SubmissionStatus NewStatus
);