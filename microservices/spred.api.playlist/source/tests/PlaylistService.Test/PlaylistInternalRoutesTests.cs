using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PlaylistService.Test.Fixtures;
using PlaylistService.Test.Helpers;
using PlaylistService.Models.Entities;
using Spred.Bus.DTOs;

namespace PlaylistService.Test;

public class PlaylistInternalRoutesTests : IClassFixture<PlaylistApiFactory>
{
    private readonly HttpClient _client;
    private readonly PlaylistApiFactory _factory;
    
    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="factory"></param>
    public PlaylistInternalRoutesTests(PlaylistApiFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task Post_InternalPlaylist_Creates_New_Metadata_Returns_200()
    {
        var dto = new MetadataDto
        {
            PrimaryId = "test:test:internal-test-id",
            Name = "Internal test",
            Description = "Created by internal route",
            Type = "playlist"
        };

        _factory.SetupPersistenceStoreMock<CatalogMetadata, Guid, long>(
            _factory.CatalogDataMock,
            CatalogMetadataHelper.InitTestObject
        );

        var authorId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync($"/internal/playlist/{authorId}", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var idString = result.GetProperty("id").GetString();
        var ok = Guid.TryParse(idString ?? string.Empty, out var id);
        Assert.True(ok);
        Assert.True(id != Guid.Empty);;
    }

    [Fact]
    public async Task Post_InternalPlaylist_Already_Exists_Returns_Existing_Id()
    {
        _factory.SetupPersistenceStoreMock<CatalogMetadata, Guid, long>(
            _factory.CatalogDataMock,
            CatalogMetadataHelper.InitTestObject
        );

        var dto = new MetadataDto
        {
            PrimaryId = "test:test:empty",
            Name = "Duplicate",
            Description = "Should return existing",
            Type = "playlist",
        };

        var authorId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync($"/internal/playlist/{authorId}", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var idString = result.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(idString));
    }

    [Fact]
    public async Task Post_InternalPlaylist_With_Invalid_Type_Returns_400()
    {
        var dto = new MetadataDto
        {
            PrimaryId = "invalid-type",
            Type = "unknown"
        };

        var authorId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync($"/internal/playlist/{authorId}", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_InternalPlaylist_By_Id_Returns_Metadata()
    {
        _factory.SetupPersistenceStoreMock<CatalogMetadata, Guid, long>(
            _factory.CatalogDataMock,
            CatalogMetadataHelper.InitTestObject
        );

        var authorId = Guid.Empty;
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/internal/playlist/{authorId}/{CatalogMetadataHelper.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MetadataDto>();
        Assert.NotNull(result);
        Assert.Equal("test:playlist:empty", result!.PrimaryId);
    }
}
