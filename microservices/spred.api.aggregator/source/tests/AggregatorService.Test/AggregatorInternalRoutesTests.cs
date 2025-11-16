using System.Net;
using System.Net.Http.Json;
using AggregatorService.Test.Fixtures;
using FluentAssertions;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;

namespace AggregatorService.Test;

public class AggregatorInternalRoutesTests : IClassFixture<AggregateServiceApiFactory>
{
    private readonly HttpClient _client;
    private readonly AggregateServiceApiFactory _factory;

    public AggregatorInternalRoutesTests(AggregateServiceApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostCatalogInfer_Should_Send_Inference_Request()
    {
        var request = new AggregateCatalogReport
        {
            Bucket = 1,
            Id = Guid.NewGuid(),
            Type = "test",
            Data = "2025-07-31"
        };

        var response = await _client.PostAsJsonAsync("/iternal/aggregator/catalog/infer", request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}