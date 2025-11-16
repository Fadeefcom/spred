using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Authorization.Test.Fixtures;
using Extensions.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Authorization.Test.Integrated;

 public class AccountRoutesTests : IClassFixture<AuthorizationApiFactory>
{
    private readonly HttpClient _client;
    private readonly AuthorizationApiFactory _factory;
    private readonly IGetToken _tokenProvider;

    public AccountRoutesTests(AuthorizationApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _tokenProvider = factory.Services.GetRequiredService<IGetToken>();
    }

    [Fact]
    public async Task GetAccounts_ShouldReturnOk_WithList()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Empty);

        var resp = await _client.GetAsync("/user/accounts");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task AddAccount_ShouldReturnCreated_WhenManagerCreates()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Empty);
        
        var body = new { platform = "spotify", accountId = "acc-x" };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var resp = await _client.PostAsync("/user/accounts", content);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("acc-x", doc.RootElement.GetProperty("accountId").GetString());
    }

    [Fact]
    public async Task AddAccount_ShouldReturnBadRequest()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Empty);

        var body = new { platform = "soundcloud", accountId = "acc-x" };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var resp = await _client.PostAsync("/user/accounts", content);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

    }

    [Fact]
    public async Task GetToken_ShouldReturnOk_WhenCreated()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.PostAsync("/user/accounts/acc-1/token", new StringContent("", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetToken_ShouldReturnBadRequest_WhenNotCreated()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.PostAsync("/user/accounts/acc-2/token", new StringContent("", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("acc-2", doc.RootElement.GetProperty("accountId").GetString());
        Assert.Contains("Unable to create verification token", json);
    }

    [Fact]
    public async Task Verify_ShouldReturnAccepted_WhenManagerReturnsTrue()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.PostAsync("/user/accounts/acc-3/verify", new StringContent("", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Verify_ShouldReturnBadRequest_WhenManagerReturnsFalse()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.PostAsync("/user/accounts/acc-4/verify", new StringContent("", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("acc-4", doc.RootElement.GetProperty("accountId").GetString());
        Assert.Equal("Verification already in progress.", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenDeleted()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.DeleteAsync("/user/accounts/acc-5");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }

    [Fact]
    public async Task Delete_ShouldReturnBadRequest_WhenNotDeleted()
    {
        var token = await _tokenProvider.GetExternalTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.DeleteAsync("/user/accounts/acc-6");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
    }
}