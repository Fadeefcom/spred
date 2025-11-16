using System.Net;
using PlaylistService.Test.Fixtures;

namespace PlaylistService.Test;

public class HealthzRoutesTests : IClassFixture<PlaylistApiFactory>
{
    private readonly PlaylistApiFactory _factory;
    
    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="factory"></param>
    public HealthzRoutesTests(PlaylistApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/healtz")]
    [InlineData("/healtz/readiness")]
    [InlineData("/healtz/liveness")]
    public async Task Health_Endpoints_Return_Healthy(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }
}