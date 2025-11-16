using MediatR;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Interfaces;
using SubmissionService.Models.Entities;
using SubmissionService.Models.Models;
using SubmissionService.Models.Queries;

namespace SubmissionService.Components.Handlers.QueryHandlers;

/// <summary>
/// Handles the query to retrieve submission statistics for a given user.
/// </summary>
/// <remarks>
/// This class is responsible for processing the <see cref="GetSubmissionStats"/> query and
/// returning aggregated submission statistics, including total submissions, pending,
/// accepted, declined submissions, as well as submissions from the current week and month.
/// </remarks>
/// <example>
/// This class is typically invoked as part of a mediator-based implementation of the CQRS pattern.
/// The handler processes data from an underlying persistence store and aggregates submission
/// statistics grouped by catalog item.
/// </example>
public sealed class GetSubmissionsStats : IRequestHandler<GetSubmissionStats, IReadOnlyList<SubmissionStatsDto>>
{
    private readonly IPersistenceStore<Submission, Guid> _submission;

    /// Handles the query for retrieving statistics about submissions.
    /// This class processes the `GetSubmissionStats` request, aggregating
    /// data such as total submissions, pending, accepted, declined, and
    /// submissions over specific timeframes (weekly and monthly).
    public GetSubmissionsStats(IPersistenceStore<Submission, Guid> submission)
    {
        _submission = submission;
    }

    /// Handles the request to retrieve submission statistics based on a specific user ID.
    /// Processes submissions for the given user and calculates statistical metrics such as total, pending, accepted,
    /// declined, weekly, and monthly submissions.
    /// <param name="request">The request object containing the user ID for whom statistics are being calculated.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A read-only list of SubmissionStatsDto objects containing submission statistics for catalog items.</returns>
    public async Task<IReadOnlyList<SubmissionStatsDto>> Handle(GetSubmissionStats request, CancellationToken cancellationToken)
    {
        var source = _submission.GetAllAsync(new PartitionKey(request.SpredUserID.ToString()), cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var startOfWeek = StartOfIsoWeekUtc(now);
        var startOfMonth = new DateTimeOffset(new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc));

        var acc = new Dictionary<Guid, Acc>();

        await foreach (var s in source.WithCancellation(cancellationToken))
        {
            var pid = s.CatalogItemId;

            if (!acc.TryGetValue(pid, out var a))
            {
                a = new Acc();
                acc[pid] = a;
            }

            a.Total++;

            switch (s.Status)
            {
                case SubmissionStatus.Created:
                    a.Pending++;
                    break;
                case SubmissionStatus.Approved:
                    a.Accepted++;
                    break;
                case SubmissionStatus.Rejected:
                    a.Declined++;
                    break;
            }

            var created = s.CreatedAt;
            if (created >= startOfWeek) a.Week++;
            if (created >= startOfMonth) a.Month++;
        }

        var items = acc.Select(kvp =>
            new SubmissionStatsDto(
                kvp.Key,
                kvp.Value.Total,
                kvp.Value.Pending,
                kvp.Value.Accepted,
                kvp.Value.Declined,
                kvp.Value.Week,
                kvp.Value.Month))
            .ToArray();

        return items;
    }

    /// Calculates the start of the ISO week (Monday as the first day of the week) in UTC time for a given date.
    /// <param name="dt">The input date and time to compute the start of the ISO week, expressed in UTC.</param>
    /// <returns>A DateTimeOffset representing the start of the ISO week (Monday at 00:00 UTC) for the specified date.</returns>
    private static DateTimeOffset StartOfIsoWeekUtc(DateTimeOffset dt)
    {
        var utc = dt.UtcDateTime;
        var diff = ((7 + (int)utc.DayOfWeek - (int)DayOfWeek.Monday) % 7);
        var sow = utc.Date.AddDays(-diff);
        return new DateTimeOffset(sow, TimeSpan.Zero);
    }

    /// <summary>
    /// Represents a data structure used for tracking submission statistics.
    /// </summary>
    private sealed class Acc
    {
        /// <summary>
        /// Represents the cumulative count of all submissions processed, regardless of their status or time frame.
        /// </summary>
        public int Total;

        /// <summary>
        /// Represents the count of submissions that are currently in a "Pending" state.
        /// Pending refers to submissions with a status of <see cref="SubmissionStatus.Created"/>.
        /// </summary>
        public int Pending;

        /// <summary>
        /// Represents the number of submissions that have been approved or accepted.
        /// This count is incremented when a submission's status changes to <see cref="SubmissionStatus.Approved"/>.
        /// </summary>
        public int Accepted;

        /// <summary>
        /// Represents the count of submissions that were declined.
        /// </summary>
        public int Declined;

        /// <summary>
        /// Represents the number of submissions recorded during the current week.
        /// This value is calculated based on the submissions created within the ISO week
        /// starting from the most recent Monday at 00:00 UTC through the current date and time.
        /// </summary>
        public int Week;

        /// <summary>
        /// Represents the month-based aggregation of submissions,
        /// tracking the number of submissions created within the current calendar month.
        /// </summary>
        public int Month;
    }
}
