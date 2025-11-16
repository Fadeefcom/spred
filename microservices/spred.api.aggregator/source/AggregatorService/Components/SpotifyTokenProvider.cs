using AggregatorService.Abstractions;
using AggregatorService.Configurations;
using Microsoft.Extensions.Options;

namespace AggregatorService.Components;

public sealed class SpotifyTokenProvider : ISpotifyTokenProvider
{
    private readonly ISpotifyAuthApi _auth;
    private readonly SpotifyCredentialsList _credentials;

    public SpotifyTokenProvider(ISpotifyAuthApi auth, IOptions<SpotifyCredentialsList> credentials)
    {
        _auth = auth;
        _credentials = credentials.Value;
    }

    public Task<(string Bearer, int CredIndex)> AcquireAsync()
        => AcquireByIndexAsync(0);

    public Task<(string Bearer, int CredIndex)> RotateAsync(int failedIndex)
        => AcquireByIndexAsync((failedIndex + 1) % _credentials.Credentials.Count);

    private async Task<(string Bearer, int CredIndex)> AcquireByIndexAsync(int startIndex)
    {
        var attempts = _credentials.Credentials.Count;
        var idx = startIndex;
        for (var i = 0; i < attempts; i++)
        {
            var c = _credentials.Credentials[idx];
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = c.ClientId,
                ["client_secret"] = c.ClientSecret
            };
            var resp = await _auth.GetToken(form);
            if (resp is { IsSuccessStatusCode: true, Content: not null })
                return ($"Bearer {resp.Content.AccessToken}", idx);
            idx = (idx + 1) % attempts;
        }
        throw new InvalidOperationException("Unable to acquire Spotify token from any credential.");
    }
}