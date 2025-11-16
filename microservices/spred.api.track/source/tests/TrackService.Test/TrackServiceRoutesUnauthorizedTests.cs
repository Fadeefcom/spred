using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TrackService.Test.Fixtures;

namespace TrackService.Test;


public class TrackServiceRoutesUnauthorizedTests
{
    private readonly HttpClient _client;

    public TrackServiceRoutesUnauthorizedTests()
    {
        var factory = new TrackServiceApiFactory
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
        new object[] { HttpMethod.Get,    "/track" },
        new object[] { HttpMethod.Get,    "/track/audio/00000000-0000-0000-0000-000000000000" },
        new object[] { HttpMethod.Get,    "/track/00000000-0000-0000-0000-000000000000" },
        new object[] { HttpMethod.Get,    "/track/spotify/00000000-0000-0000-0000-000000000000/00000000-0000-0000-0000-000000000000" },
        new object[] { HttpMethod.Delete, "/track/00000000-0000-0000-0000-000000000000" },
        new object[] { HttpMethod.Patch,  "/track/00000000-0000-0000-0000-000000000000" },
        new object[] { HttpMethod.Post,   "/track" },
    };

    [Theory(DisplayName = "Unauthorized access returns 401")]
    [MemberData(nameof(SecuredEndpoints))]
    public async Task Secured_Endpoints_Should_Return_401(HttpMethod method, string url)
    {
        // Arrange
        var request = new HttpRequestMessage(method, url);

        if (method == HttpMethod.Post)
        {
            var jsonData = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"));
            request.Headers.Add("X-JSON-Data", jsonData);
            request.Content = new MultipartFormDataContent();
        }
        else if (method == HttpMethod.Patch)
        {
            var json = JsonSerializer.Serialize(new { name = "test" });
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}