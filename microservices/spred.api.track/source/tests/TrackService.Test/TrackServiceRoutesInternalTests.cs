using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Spred.Bus.DTOs;
using TrackService.Models.Entities;
using TrackService.Test.Fixtures;

namespace TrackService.Test;

public class TrackServiceRoutesInternalTests : IClassFixture<TrackServiceApiFactory>
{
    private readonly HttpClient _client;
    private readonly TrackServiceApiFactory _factory;

    public TrackServiceRoutesInternalTests(TrackServiceApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "service-token");
    }

    [Fact]
    public async Task Get_InternalAudio_Returns200Or404()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/internal/track/audio/{id}/{id}");
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_CheckAudioExists_Returns200Or404()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/internal/track/audio/exists/{id}/{id}");
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_CreateMetadata_Returns200AndId()
    {
        var dto = new TrackDto
        {
            Title = "Test",
            Description = "Desc",
            PrimaryId = "pid",
            Id = Guid.NewGuid()
        };

        var json = JsonSerializer.Serialize(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/internal/track/{Guid.Empty}", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseJson = await response.Content.ReadAsStringAsync();
        Assert.Contains("id", responseJson);
    }
    
    [Fact]
    public async Task Post_CreateMetadata2_Returns200AndId()
    {
        var dto = new TrackDtoWithPlatformIds()
        {
            Title = "Test",
            Description = "Desc",
            PrimaryId = "pid:1:2",
            Id = Guid.Empty
        };

        var json = JsonSerializer.Serialize(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/internal/track/{Guid.Empty}", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseJson = await response.Content.ReadAsStringAsync();
        Assert.Contains("id", responseJson);
    }

    [Fact]
    public async Task Patch_UploadAudio_Returns201()
    {
        var id = Guid.NewGuid();

        var content = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes("dummy-audio");
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        content.Add(fileContent, "file", "test.mp3");

        var response = await _client.PatchAsync($"/internal/track/{id}/{id}", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
    
    [Fact]
    public async Task Patch_UploadAudio_Returns201_2()
    {
        var id = Guid.Empty;

        var content = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes("dummy-audio");
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
        content.Add(fileContent, "file", "test.mp3");

        var response = await _client.PatchAsync($"/internal/track/{id}/{id}", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Get_PublicTrack_Returns200Or404()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var response = await _client.GetAsync($"/track/internal/{userId}/{id}");
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task Patch_Track_Returns201_2()
    {
        var id = Guid.Empty;
        await _client.PatchAsync($"/internal/track/{id}/{id}/unsuccessful", null);
        await _client.PatchAsync($"/internal/track/{id}/{id}/unsuccessful", null);
        await _client.PatchAsync($"/internal/track/{id}/{id}/unsuccessful", null);
        var response = await _client.PatchAsync($"/internal/track/{id}/{id}/unsuccessful", null);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _factory.CatalogDataMock.Verify(store => store.UpdateAsync(
            It.Is<TrackMetadata>(track =>
                track.Id == id &&
                track.Status == UploadStatus.Failed
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}