using System.Text.Json;
using InferenceService.Models.Dto;
using Refit;

namespace InferenceService.Abstractions;

/// <summary>
/// Provides vector search operations for catalogs.
/// </summary>
public interface IVectorSearch
{
    /// <summary>
    /// Searches catalogs using vector similarity.
    /// </summary>
    /// <param name="query">Search parameters including vector and filters.</param>
    /// <returns>API response with search results as raw JSON.</returns>
    [Post("/index/search/catalogs")]
    public Task<IApiResponse<JsonElement>> SearchCatalogs(SearchQuery query);
}