namespace SubmissionService.Models.Commands;

/// <summary>
/// Result returned after successfully creating a new submission.
/// Provides identifiers of the created submission, the related users,
/// and a snapshot of the submission state.
/// </summary>
/// <param name="SubmissionId">
/// The unique identifier of the newly created submission.
/// </param>
/// <param name="ArtistUserId">
/// The identifier of the artist who initiated the submission.
/// </param>
/// <param name="CuratorUserId">
/// The identifier of the curator associated with the submission's catalog item.
/// </param>
/// <param name="AfterSnapshot">
/// A snapshot object representing the state of the submission
/// immediately after it was created. Typically contains whitelisted fields
/// relevant for activity/audit records.
/// </param>
public sealed record SubmissionCreatedResult(
    Guid SubmissionId,
    Guid ArtistUserId,
    Guid CuratorUserId,
    object AfterSnapshot
);
