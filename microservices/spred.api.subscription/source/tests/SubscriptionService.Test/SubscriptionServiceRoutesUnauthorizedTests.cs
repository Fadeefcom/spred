using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SubscriptionService.Test.Fixtures;

namespace SubscriptionService.Test;

public class SubscriptionServiceRoutesUnauthorizedTests
{
    private readonly HttpClient _client;

    public SubscriptionServiceRoutesUnauthorizedTests()
    {
        var factory = new SubscriptionApiFactory()
        {
            EnableTestAuth = false
        };

        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public static IEnumerable<object[]> SecuredEndpoints => new List<object[]>
    {
        // Public group
        new object[] { HttpMethod.Post, "/subscription/checkout" },
        new object[] { HttpMethod.Post, "/subscription/cancel" },
        new object[] { HttpMethod.Get,  "/subscription/status" }
    };

    [Theory(DisplayName = "Unauthorized subscription endpoints return 401")]
    [MemberData(nameof(SecuredEndpoints))]
    public async Task Secured_Endpoints_Should_Return_401(HttpMethod method, string url)
    {
        // Arrange
        var request = new HttpRequestMessage(method, url);

        if (method == HttpMethod.Post)
        {
            object body = url switch
            {
                "/subscription/checkout" => new { plan = "pro" },
                "/subscription/cancel" => new { subscriptionId = Guid.NewGuid() },
                "/internal/stripe/webhook" => new { test = true },
                _ => new { }
            };

            var json = JsonSerializer.Serialize(body);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
