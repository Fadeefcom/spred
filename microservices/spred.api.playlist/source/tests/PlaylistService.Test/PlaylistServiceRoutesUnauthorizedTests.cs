using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PlaylistService.Test.Fixtures;

namespace PlaylistService.Test;

public class PlaylistServiceRoutesUnauthorizedTests
{
    private readonly HttpClient _client;

    public PlaylistServiceRoutesUnauthorizedTests()
    {
        var factory = new PlaylistApiFactory()
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
        new object[] { HttpMethod.Get,    "/playlist" },
        new object[] { HttpMethod.Get,    "/playlist/22222222-2222-2222-2222-222222222222/11111111-1111-1111-1111-111111111111" },
        new object[] { HttpMethod.Post,   "/playlist" },
        new object[] { HttpMethod.Patch,  $"/playlist/{Guid.NewGuid()}" },
        new object[] { HttpMethod.Delete, $"/playlist/{Guid.NewGuid()}" }
    };

    [Theory(DisplayName = "Unauthorized playlist access returns 401")]
    [MemberData(nameof(SecuredEndpoints))]
    public async Task Secured_Endpoints_Should_Return_401(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);

        if (method == HttpMethod.Post || method == HttpMethod.Patch)
        {
            var dto = new { name = "test", primaryId = "empty", description = "desc" };
            var json = JsonSerializer.Serialize(dto);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}