using MediatR;
using SubmissionService.Abstractions;
using SubmissionService.Models.Entities;

namespace SubmissionService.Models.Commands;

/// <summary>
/// Command for updating the status of an existing submission
/// and generating corresponding activity records for both the artist
/// and the curator involved.
/// </summary>
/// <param name="SubmissionId">
/// The unique identifier of the submission to be updated.
/// </param>
/// <param name="ArtistId">
/// The identifier of the artist who owns the submission.
/// </param>
/// <param name="CatalogItemId">
/// The identifier of the catalog item associated with the submission.
/// </param>
/// <param name="NewStatus">
/// The new status to assign to the submission.
/// </param>
/// <returns>
/// A <see cref="SubmissionStatusUpdatedResult"/> containing the old and new status values,
/// along with identifiers of the affected entities.
/// </returns>
/// <remarks>
/// This command implements <see cref="IAuditableCommand{TResult}"/> and produces two activity records:
/// one for the artist and one for the curator. Each activity includes <c>before</c> and <c>after</c>
/// snapshots of the submission status, along with categorization tags.
/// </remarks>
public sealed record UpdateSubmissionStatusCommand(
    Guid SubmissionId,
    Guid ArtistId,
    Guid CatalogItemId,
    SubmissionStatus NewStatus)
    : IRequest<SubmissionStatusUpdatedResult>, IAuditableCommand<SubmissionStatusUpdatedResult>
{
    /// <inheritdoc/>
    public IEnumerable<ActivityDescriptor> ToActivities(SubmissionStatusUpdatedResult? handlerResult)
    {
        if (handlerResult is null) yield break;
        var args = new Dictionary<string, object?> { ["status"] = handlerResult.NewStatus.ToString() };
        var before = new { status = handlerResult.OldStatus.ToString() };
        var after = new { status = handlerResult.NewStatus.ToString() };
        var tags = new[] { "submission", "status_changed", handlerResult.NewStatus.ToString().ToLowerInvariant() };

        yield return new ActivityDescriptor("status_changed", "submission", handlerResult.SubmissionId,
            $"submission.status_changed.{handlerResult.NewStatus.ToString().ToLowerInvariant()}", args,
            Spred.Bus.Contracts.ActivityImportance.Important, handlerResult.ArtistUserId, handlerResult.CuratorUserId,
            before, after, tags);
        
        yield return new ActivityDescriptor("status_changed", "submission", handlerResult.SubmissionId,
            $"submission.status_changed.{handlerResult.NewStatus.ToString().ToLowerInvariant()}", args,
            Spred.Bus.Contracts.ActivityImportance.Important, handlerResult.CuratorUserId, handlerResult.ArtistUserId,
            before, after, tags);
    }
}