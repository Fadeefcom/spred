using ActivityService.Abstractions;
using ActivityService.Models;

namespace ActivityService.Components.Services;

/// <inheritdoc/>
public sealed class ActivityMessageFormatter : IActivityMessageFormatter
{
    /// <inheritdoc/>
    public string Format(ActivityEntity activity)
    {
        return activity.MessageKey switch
        {
            "submission.created" =>
                $"You submitted track {activity.Args["trackName"]} to catalog {activity.Args["catalogName"]}.",

            var key when key.StartsWith("submission.status_changed", StringComparison.Ordinal) =>
                $"Submission status changed from {(activity.Before is not null ? ((dynamic)activity.Before)?.status : "unknown")} " +
                $"to {((dynamic)activity.After!)?.status}.",

            "user.display_name_changed" =>
                $"Your display name was updated from {activity.Args["oldName"]} to {activity.Args["newName"]}.",

            _ => $"[{activity.Verb}] {activity.ObjectType} {activity.ObjectId}"
        };
    }
}