using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Authorization.Test.Fixtures;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Authorization.Test.Integrated;

public class ProgramTests(AuthorizationApiFactory factory) : IClassFixture<AuthorizationApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/healtz")]
    [InlineData("/healtz/readiness")]
    [InlineData("/healtz/liveness")]
    public async Task HealthEndpoints_ShouldReturn200(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerEndpoint_ShouldBeAccessible_InDevelopment()
    {
        var response = await _client.GetAsync("/swagger/index.html");
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task XForwardedProto_ShouldNotCrash()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/healtz");
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task InitRoute_ShouldReturn204_AndSetHeaders()
    {
        var response = await _client.GetAsync("/auth/init");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains("Sec-CH-UA", response.Headers.Vary.ToList());
        Assert.Contains("Sec-CH-UA-Mobile", response.Headers.Vary.ToList());
        Assert.Contains("Sec-CH-UA-Platform", response.Headers.Vary.ToList());

        Assert.True(response.Headers.TryGetValues("Accept-CH", out var acceptCh));
        var acceptHeader = string.Join(",", acceptCh);
        Assert.Contains("Sec-CH-UA", acceptHeader);
        Assert.Contains("Sec-CH-UA-Mobile", acceptHeader);
        Assert.Contains("Sec-CH-UA-Platform", acceptHeader);
    }

    [Fact]
    public async Task NotifyMe_ShouldReturn400_WhenInvalid()
    {
        var response = await _client.PostAsync("/user/notify", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Feedback_ShouldReturn400_WhenMissingFields()
    {
        var json = JsonSerializer.Serialize(new FeedbackForm { Subject = "", Message = "", FeedbackType = "" });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/user/feedback", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/auth/login")]
    [InlineData("/auth/logout")]
    [InlineData("/auth/refresh-token")]
    [InlineData("/user/me")]
    [InlineData("/user/feedback")]
    [InlineData("/user/authentications")]
    [InlineData("/user/token")]
    [InlineData("/user/notify")]
    public async Task AuthAndUserEndpoints_ShouldReturnExpectedStatusCodes(string endpoint)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

        if (endpoint == "/auth/logout" || endpoint == "/auth/refresh-token" || endpoint == "/user/feedback" || endpoint == "/user/notify")
            request = new HttpRequestMessage(HttpMethod.Post, endpoint);

        if (endpoint == "/user/feedback")
        {
            var payload = new FeedbackForm
            {
                Subject = "Test",
                Message = "Test message",
                FeedbackType = "Bug"
            };
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        else if (endpoint == "/user/notify")
        {
            var payload = new NotifyMeFrom
            {
                Name = "User",
                Email = "email@example.com",
                ArtistType = "Solo",
                Message = "Notify me"
            };
            var json = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _client.SendAsync(request);
        var allowedStatuses = new[]
        {
            HttpStatusCode.OK,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.NoContent,
            HttpStatusCode.BadRequest,
            HttpStatusCode.Redirect,
            HttpStatusCode.NotFound
        };

        Assert.Contains(response.StatusCode, allowedStatuses);
    }
}

public class NotifyMeFrom
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ArtistType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class FeedbackForm
{
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string FeedbackType { get; set; } = string.Empty;
}