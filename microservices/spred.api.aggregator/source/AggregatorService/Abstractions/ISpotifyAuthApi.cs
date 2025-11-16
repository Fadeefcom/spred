using AggregatorService.Models.Dto;
using Refit;

namespace AggregatorService.Abstractions;

public interface ISpotifyAuthApi
{
    [Post("/api/token")]
    [Headers("Content-Type: application/x-www-form-urlencoded")]
    Task<IApiResponse<TokenResponse>> GetToken(
        [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> form);

}