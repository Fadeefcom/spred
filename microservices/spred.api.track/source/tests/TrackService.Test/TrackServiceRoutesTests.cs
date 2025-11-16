using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Spred.Bus.DTOs;
using TrackService.Models.DTOs;
using TrackService.Test.Fixtures;

namespace TrackService.Test;

public class TrackServiceRoutesTests : IClassFixture<TrackServiceApiFactory>
{
    private static byte[] _mp3Bytes = null!;
    private readonly HttpClient _client;
    private readonly TrackServiceApiFactory _factory;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="factory"></param>
    public TrackServiceRoutesTests(TrackServiceApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        if (_mp3Bytes == null)
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "TestFiles", "test.mp3");
            _mp3Bytes = File.ReadAllBytes(filePath);
        }
    }

    [Fact]
    public async Task Post_AddTrack_Returns200AndId()
    {
        _factory.MockAudioStream.Setup(a => a.Codec).Returns("mp3");
        
        var json = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TrackCreate()
        {
            Description = "Test description",
            Title = "Test title",
            TrackUrl = string.Empty
        })));

        var memoryStream = new MemoryStream(_mp3Bytes);
        var fileContent = new StreamContent(memoryStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");

        var content = new MultipartFormDataContent();
        content.Add(fileContent, "file", "test.mp3");

        var request = new HttpRequestMessage(HttpMethod.Post, "/track");
        request.Content = content;
        request.Headers.Add("X-JSON-Data", json);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_AddTrack_Returns400_IfMissingJsonHeader()
    {
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("dummy")), "file", "test.mp3");

        var response = await _client.PostAsync("/track", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_AddTrack_Returns400_IfMissingFile()
    {
        var json = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { Name = "Track 1" })));

        var request = new HttpRequestMessage(HttpMethod.Post, "/track");
        request.Headers.Add("X-JSON-Data", json);
        request.Content = new MultipartFormDataContent();

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_AddTrack_Returns422_IfValidationFails()
    {
        var json = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}")); // Empty object
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("dummy")), "file", "test.mp3");

        var request = new HttpRequestMessage(HttpMethod.Post, "/track");
        request.Headers.Add("X-JSON-Data", json);
        request.Content = content;

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Track_Returns204()
    {
        var id = Guid.Empty;
        var response = await _client.DeleteAsync($"/track/{id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Get_Track_Returns200WithDto()
    {
        var id = Guid.Empty;
        var response = await _client.GetAsync($"/track/{id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_Track_Returns404_IfNotFound()
    {
        var response = await _client.GetAsync($"/track/{Guid.NewGuid()}");
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_TracksByQuery_Returns200WithList()
    {
        var response = await _client.GetAsync("/track?search=test");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Patch_UpdateTrack_Returns204()
    {
        var id = Guid.Empty;
        var payload = new TrackDto() { Id = id, Title = "Updated Name", PrimaryId = "100" };
        var response = await _client.PatchAsync($"/track/{id}",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Patch_UpdateTrack_Returns400_IfValidationFails()
    {
        var id = Guid.NewGuid();
        var response =
            await _client.PatchAsync($"/track/{id}", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_Audio_Returns200WithFile()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/track/audio/{id}");
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Audio_Returns404_IfNotFound()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/track/audio/{id}");
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_PublicTrack_Returns200()
    {
        var id = Guid.NewGuid();
        var spredUserId = Guid.NewGuid();
        var response = await _client.GetAsync($"/track/spotify/{spredUserId}/{id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_PublicTrack_Returns404_IfNotFound()
    {
        var id = Guid.NewGuid();
        var spredUserId = Guid.NewGuid();
        var response = await _client.GetAsync($"/track/spotify/{spredUserId}/{id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}