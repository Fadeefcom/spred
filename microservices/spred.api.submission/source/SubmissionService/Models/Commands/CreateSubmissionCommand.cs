using MediatR;
using SubmissionService.Abstractions;

namespace SubmissionService.Models.Commands;

/// <summary>
/// Command for creating a new submission and generating corresponding
/// activity records for both the artist and the curator involved.
/// </summary>
/// <param name="Request">
/// The request payload containing submission details, including track,
/// catalog, and curator identifiers.
/// </param>
/// <returns>
/// A <see cref="SubmissionCreatedResult"/> containing the identifier of the created submission,
/// the associated users, and a snapshot of the created entity.
/// </returns>
/// <remarks>
/// This command implements <see cref="IAuditableCommand{TResult}"/> and produces two activity records:
/// one for the artist and one for the curator. Each activity includes
/// the <c>after</c> snapshot of the submission, categorization tags, and
/// role-specific ownership information.
/// </remarks>
public sealed record CreateSubmissionCommand(CreateSubmissionRequest Request)
    : IRequest<SubmissionCreatedResult>, IAuditableCommand<SubmissionCreatedResult>
{
    /// <inheritdoc/>
    public IEnumerable<ActivityDescriptor> ToActivities(SubmissionCreatedResult? handlerResult)
    {
        if(handlerResult is null) yield break;
        
        var args = new Dictionary<string, object?>
        {
            ["trackId"] = Request.TrackId, 
            ["catalogItemId"] = Request.CatalogItemId,
            ["catalogName"] = handlerResult.AfterSnapshot.GetType().GetProperty("catalogName")?.GetValue(handlerResult.AfterSnapshot),
            ["trackName"] = handlerResult.AfterSnapshot.GetType().GetProperty("trackName")?.GetValue(handlerResult.AfterSnapshot),
        };
        var tags = new[] { "submission", "created" };

        yield return new ActivityDescriptor("created", "submission", handlerResult.SubmissionId, 
            "submission.created", args, Spred.Bus.Contracts.ActivityImportance.Normal,
            handlerResult.ArtistUserId, handlerResult.CuratorUserId, null, handlerResult.AfterSnapshot, tags);
        
        yield return new ActivityDescriptor(
            "received", "submission", handlerResult.SubmissionId,
            "submission.received", args, Spred.Bus.Contracts.ActivityImportance.Normal,
            handlerResult.CuratorUserId,
            handlerResult.ArtistUserId,
            null, handlerResult.AfterSnapshot, tags);
    }
}