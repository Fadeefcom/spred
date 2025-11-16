using SubmissionService.Models;

namespace SubmissionService.Abstractions;

/// <summary>
/// Defines a contract for commands that produce auditable activity records.
/// </summary>
/// <remarks>
/// Implementing this interface allows a command to generate one or more
/// <see cref="ActivityDescriptor"/> instances representing domain activities
/// that occurred during the execution of the command handler.
/// These activity descriptors are processed by the activity pipeline behavior
/// to create persistent <c>ActivityRecord</c> entries for auditing and user-facing history.
/// </remarks>
public interface IAuditableCommand<in TResult>
{
    /// <summary>
    /// Produces one or more activity descriptors that capture the effect
    /// of executing the command handler.
    /// </summary>
    /// <param name="handlerResult">
    /// The result returned by the command handler, if any.
    /// Implementations may use this to enrich activity descriptors with
    /// before/after snapshots, identifiers, or computed values.
    /// </param>
    /// <returns>
    /// A sequence of <see cref="ActivityDescriptor"/> objects representing
    /// activities triggered by the command. May be empty if no activities
    /// should be recorded.
    /// </returns>
    IEnumerable<ActivityDescriptor> ToActivities(TResult? handlerResult);
}