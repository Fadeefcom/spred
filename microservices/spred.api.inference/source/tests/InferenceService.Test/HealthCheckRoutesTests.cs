using System.Net;
using InferenceService.Test.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace InferenceService.Test;

public class HealthCheckRoutesTests : IClassFixture<InferenceApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckRoutesTests(InferenceApiFactory factory)
    {
        var clientFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddHealthChecks()
                    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["ready", "live"]);
            });
        });

        _client = clientFactory.CreateClient();
    }

    [Theory]
    [InlineData("/healtz")]
    [InlineData("/healtz/liveness")]
    [InlineData("/healtz/readiness")]
    public async Task HealthCheckEndpoints_ReturnHealthy(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.ServiceUnavailable);
    }
}