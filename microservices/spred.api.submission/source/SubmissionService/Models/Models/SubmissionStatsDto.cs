namespace SubmissionService.Models.Models;

public sealed record SubmissionStatsDto(
    Guid CatalogId,
    int TotalSubmissions,
    int PendingSubmissions,
    int AcceptedSubmissions,
    int DeclinedSubmissions,
    int WeeklySubmissions,
    int MonthlySubmissions
);