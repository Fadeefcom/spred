namespace AggregatorService.Configurations;

public sealed record SpotifyCredentialsList
{
    public List<SpotifyCredential> Credentials { get; init; } = new();
}

public sealed record SpotifyCredential
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}