using System.Net;
using AggregatorService.Test.Fixtures;

namespace AggregatorService.Test;

public class HealthzRoutesTests : IClassFixture<AggregateServiceApiFactory>
{
    private readonly AggregateServiceApiFactory _factory;
    
    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="factory"></param>
    public HealthzRoutesTests(AggregateServiceApiFactory factory)
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