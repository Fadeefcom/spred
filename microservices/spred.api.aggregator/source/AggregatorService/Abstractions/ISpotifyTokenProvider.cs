namespace AggregatorService.Abstractions;

public interface ISpotifyTokenProvider
{
    Task<(string Bearer, int CredIndex)> AcquireAsync();
    Task<(string Bearer, int CredIndex)> RotateAsync(int failedIndex);
}