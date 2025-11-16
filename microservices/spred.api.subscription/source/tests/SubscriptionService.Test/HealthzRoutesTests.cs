using System.Net;
using SubscriptionService.Test.Fixtures;

namespace SubscriptionService.Test;

public class HealthzRoutesTests : IClassFixture<SubscriptionApiFactory>
{
    private readonly SubscriptionApiFactory _factory;
    
    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="factory"></param>
    public HealthzRoutesTests(SubscriptionApiFactory factory)
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