using ActivityService.Models;

namespace ActivityService.Abstractions;

/// <summary>
/// Represents a mechanism for converting activity data into a human-readable message format.
/// </summary>
/// <remarks>
/// Classes implementing this interface should provide logic to process an instance of
/// <see cref="ActivityEntity"/> and generate a descriptive and contextually relevant string output
/// for user-facing scenarios or logs.
/// </remarks>
public interface IActivityMessageFormatter
{
    /// <summary>
    /// Converts the provided activity data into a human-readable message format.
    /// </summary>
    /// <param name="activity">The activity entity containing details to be formatted into a descriptive message.</param>
    /// <returns>A string representing a human-readable, formatted message derived from the activity entity.</returns>
    string Format(ActivityEntity activity);
}