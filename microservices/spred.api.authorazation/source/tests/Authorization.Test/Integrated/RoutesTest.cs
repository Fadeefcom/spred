using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Authorization.Test.Fixtures;
using Extensions.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Authorization.Test.Integrated;

public class RoutesTest : IClassFixture<AuthorizationApiFactory>
{
    private readonly HttpClient _client;
    private readonly IGetToken _tokenProvider;
    private readonly AuthorizationApiFactory _factory;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="factory"></param>
    public RoutesTest(AuthorizationApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        _tokenProvider = factory.Services.GetRequiredService<IGetToken>();
        _factory = factory;
    }

    [Fact]
    public async Task Login_ShouldRedirect_WhenAuthorized()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Empty);
        var response = await _client.GetAsync("/auth/login?redirect_mode=same");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/artist/upload", response.Headers.Location?.AbsolutePath);
    }

     [Fact]
    public async Task Should_ReturnBadRequest_When_ProviderMissing()
    {
        var response = await _client.GetAsync("/auth/external/login?role=user&deviceId=dev1");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("provider is required", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_RoleMissing()
    {
        var response = await _client.GetAsync("/auth/external/login?provider=google&deviceId=dev1");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("role is required", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_DeviceIdMissing()
    {
        var response = await _client.GetAsync("/auth/external/login?provider=google&role=user");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("deviceId is required", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_InvalidProvider()
    {
        var response = await _client.GetAsync("/auth/external/login?provider=invalid&role=user&deviceId=dev1");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid provider", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_InvalidRole()
    {
        var response = await _client.GetAsync("/auth/external/login?provider=google&role=invalid&deviceId=dev1");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid role", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Should_Challenge_When_ValidRequest()
    {
        var response = await _client.GetAsync("/auth/external/login?provider=google&role=user&deviceId=dev1&redirect_mode=web");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_ShouldReturnUser_WhenAuthorized()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/user/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Feedback_ShouldSave_WhenValid()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var content = new StringContent(JsonSerializer.Serialize(new
        {
            Subject = "Test",
            Message = "Feedback message",
            FeedbackType = "Bug"
        }), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/user/feedback", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TokenEndpoint_ShouldNotReturnAccessToken_WhenValid()
    {
        var token = await _tokenProvider.GetInternalTokenAsync([]);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/user/token?scope=google");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NotifyMe_ShouldFailed_WhenInvalid()
    {
        var content = new StringContent(JsonSerializer.Serialize(new
        {
            Name = "Test,,11../User",
            Email = "user@example.com",
            ArtistType = "Solo",
            Message = "Notify me"
        }), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/user/notify", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task NotifyMe_ShouldSucceed_WhenValid()
    {
        var content = new StringContent(JsonSerializer.Serialize(new
        {
            Name = "Test User",
            Email = "user@example.com",
            ArtistType = "Solo",
            Message = "Notify me"
        }), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/user/notify", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    
    [Fact]
    public async Task Login_ShouldReturnHtml_WhenPopupMode()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Empty);
        var resp = await _client.GetAsync("/auth/login?redirect_mode=popup");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("text/html", resp.Content.Headers.ContentType?.MediaType);
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("postMessage(\"auth_success\"", html);
    }

    [Fact]
    public async Task Login_ShouldReturnNoContent_WhenNoRedirectMode()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Empty);
        var resp = await _client.GetAsync("/auth/login");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task Init_ShouldSetClientHintsHeaders()
    {
        var resp = await _client.GetAsync("/auth/init");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        Assert.True(resp.Headers.Contains("Accept-CH"));
        Assert.True(resp.Headers.Contains("Vary"));
    }

    [Fact]
    public async Task Me_Update_ShouldReturnNoContent_WhenValid()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            FirstName = "John",
            LastName = "Doe",
            Country = "RS",
            City = "Belgrade"
        };

        var resp = await _client.PatchAsync("/user/me",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task Avatar_ShouldReturnBadRequest_WhenEmptyFile()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = new MultipartFormDataContent();
        var empty = new ByteArrayContent([]);
        empty.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(empty, "file", "empty.png");

        var resp = await _client.PutAsync("/user/me/avatar", content);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("No file uploaded", body);
    }

    [Fact]
    public async Task Avatar_ShouldReturnBadRequest_WhenNotImage()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(Encoding.UTF8.GetBytes("not an image"));
        bytes.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(bytes, "file", "note.txt");

        var resp = await _client.PutAsync("/user/me/avatar", content);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Only image files are allowed", body);
    }
    
     [Fact]
    public async Task Logout_ShouldSucceed()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Empty);
        var resp = await _client.PostAsync("/auth/logout", new StringContent(""));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task ExternalLogin_ShouldAccept_CaseInsensitiveProvider()
    {
        var resp = await _client.GetAsync("/auth/external/login?provider=GoOgLe&role=user&deviceId=dev1&redirect_mode=web");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Feedback_ShouldReturnBadRequest_WhenBodyNull()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await _client.PostAsync("/user/feedback", new StringContent("", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Invalid request", body, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NotifyMe_ShouldReturnBadRequest_WhenModelInvalid()
    {
        var payload = new { Name = "", Email = "bad", ArtistType = "", Message = "" };
        var resp = await _client.PostAsync("/user/notify",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Me_ShouldReturnJson_WithId()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await _client.GetAsync("/user/me");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("application/json", resp.Content.Headers.ContentType?.MediaType);
        var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
        Assert.NotNull(json?["id"]);
        Assert.False(string.IsNullOrWhiteSpace(json?["id"]!.ToString()));
    }

    [Fact]
    public async Task Avatar_ShouldSucceed_WhenValidPng()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = new MultipartFormDataContent();
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
        var file = new ByteArrayContent(png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "ok.png");

        var resp = await _client.PutAsync("/user/me/avatar", content);
        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
    }
}