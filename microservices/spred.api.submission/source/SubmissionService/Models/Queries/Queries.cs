using MediatR;
using SubmissionService.Models.Entities;
using SubmissionService.Models.Models;

namespace SubmissionService.Models.Queries;

/// <summary>
/// Query for retrieving submissions within a specific catalog,
/// optionally filtered by submission status, with paging support.
/// </summary>
/// <param name="CatalogItemId">
/// The identifier of the catalog item whose submissions should be retrieved.
/// </param>
/// <param name="Status">
/// Optional status filter to include only submissions matching the specified <see cref="SubmissionStatus"/>.
/// If <c>null</c>, all statuses are included.
/// </param>
/// <param name="Offset">
/// The number of submissions to skip for paging.
/// </param>
/// <param name="Limit">
/// The maximum number of submissions to return.
/// </param>
/// <returns>
/// A collection of <see cref="SubmissionDto"/> objects for the specified catalog item.
/// </returns>
public sealed record GetSubmissionsByCatalogQuery(Guid CatalogItemId, SubmissionStatus? Status, int Offset, int Limit) : IRequest<IEnumerable<SubmissionDto>>;

/// <summary>
/// Query for retrieving a single submission by its catalog and submission identifier.
/// </summary>
/// <param name="CatalogItemId">
/// The identifier of the catalog item that the submission belongs to.
/// </param>
/// <param name="Id">
/// The unique identifier of the submission.
/// </param>
/// <returns>
/// A <see cref="SubmissionDto"/> if found; otherwise <c>null</c>.
/// </returns>
public sealed record GetSubmissionByIdQuery(Guid CatalogItemId, Guid Id) : IRequest<SubmissionDto?>;

/// <summary>
/// Query for retrieving submissions associated with the current actor,
/// either as an artist (artist inbox) or curator (submissions),
/// optionally filtered by submission status, with paging support.
/// </summary>
/// <param name="Status">
/// Optional status filter to include only submissions matching the specified <see cref="SubmissionStatus"/>.
/// If <c>null</c>, all statuses are included.
/// </param>
/// <param name="Offset">
/// The number of submissions to skip for paging.
/// </param>
/// <param name="Limit">
/// The maximum number of submissions to return.
/// </param>
/// <returns>
/// A collection of <see cref="SubmissionDto"/> objects for the current user.
/// </returns>
public sealed record GetMySubmissionsQuery(SubmissionStatus? Status, int Offset, int Limit) : IRequest<IEnumerable<SubmissionDto>>;

public record GetSubmissionStats(Guid SpredUserID) : IRequest<IReadOnlyList<SubmissionStatsDto>>;