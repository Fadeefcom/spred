using Authorization.Models.Dto;
using Refit;

namespace Authorization.Abstractions;

/// <summary>
/// Interface for interacting with the Spotify API.
/// </summary>
public interface ISpotifyApi
{
    /// <summary>
    /// Retrieves an access token from the Spotify API.
    /// </summary>
    /// <param name="authorization">The authorization header value, typically in the format "Basic {Base64EncodedClientIdAndSecret}".</param>
    /// <param name="request">The token request containing grant type, client ID, and client secret.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the API response with the Spotify token response.</returns>
    [Post("/api/token")]
    [Headers("Content-Type: application/x-www-form-urlencoded")]
    Task<ApiResponse<SpotifyTokenResponse>> GetAccessTokenAsync([Header("Authorization")] string authorization, [Body(BodySerializationMethod.UrlEncoded)] TokenRequest request);
}
