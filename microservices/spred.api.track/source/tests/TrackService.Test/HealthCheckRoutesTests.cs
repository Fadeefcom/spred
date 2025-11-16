using System.Net;
using TrackService.Test.Fixtures;

namespace TrackService.Test;

public class HealthCheckRoutesTests : IClassFixture<TrackServiceApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckRoutesTests(TrackServiceApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/healtz")]
    [InlineData("/healtz/readiness")]
    [InlineData("/healtz/liveness")]
    public async Task Health_Endpoints_ShouldReturn_200Or503(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Unexpected status code {response.StatusCode} for {url}"
        );
    }
}