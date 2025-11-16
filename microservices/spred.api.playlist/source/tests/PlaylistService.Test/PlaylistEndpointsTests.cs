using System.Net;
using System.Net.Http.Json;
using PlaylistService.Models.DTO;
using PlaylistService.Test.Fixtures;
using PlaylistService.Test.Helpers;
using PlaylistService.Models.Entities;

namespace PlaylistService.Test;

public class PlaylistRoutesTests : IClassFixture<PlaylistApiFactory>
{
    private readonly HttpClient _client;
    private readonly PlaylistApiFactory _factory;

    public PlaylistRoutesTests(PlaylistApiFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task Get_User_Metadata_Returns_200()
    {
        var metadata = CatalogMetadataHelper.InitTestObject();
        _factory.SetupPersistenceStoreMock<CatalogMetadata, Guid, long>(_factory.CatalogDataMock, () => metadata);

        var response = await _client.GetAsync("/playlist");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseBody = await response.Content.ReadFromJsonAsync<List<PublicMetadataDto>>();
        Assert.NotNull(responseBody);
        Assert.NotEmpty(responseBody);
    }

    [Fact]
    public async Task Get_User_Metadata_With_Invalid_Type_Returns_400()
    {
        var metadata = CatalogMetadataHelper.InitTestObject();
        _factory.SetupPersistenceStoreMock<CatalogMetadata, Guid, long>(_factory.CatalogDataMock, () => metadata);

        var response = await _client.GetAsync("/playlist?type=invalid_type");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_Public_Metadata_ById_Returns_200()
    {
        var metadata = CatalogMetadataHelper.InitTestObject();
        _factory.SetupPersistenceStoreMock<CatalogMetadata, Guid, long>(_factory.CatalogDataMock, () => metadata);

        var response = await _client.GetAsync($"/playlist/{Guid.Empty}/{CatalogMetadataHelper.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_Public_Metadata_Returns_204_When_Not_Public()
    {
        var metadata = CatalogMetadataHelper.InitTestObject2(isPublic: false);
        _factory.SetupPersistenceStoreMock<CatalogMetadata, Guid, long>(_factory.CatalogDataMock, () => metadata);

        var response = await _client.GetAsync($"/playlist/{Guid.Empty}/{CatalogMetadataHelper.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Get_Public_Metadata_ById_Returns_404()
    {
        var metadata = CatalogMetadataHelper.InitTestObject();
        _factory.SetupPersistenceStoreMock<CatalogMetadata, Guid, long>(_factory.CatalogDataMock, () => metadata);

        var response = await _client.GetAsync($"/playlist/{Guid.NewGuid()}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Metadata_By_Id_Returns_404()
    {
        var metadata = CatalogMetadataHelper.InitTestObject();
        _factory.SetupPersistenceStoreMock<CatalogMetadata, Guid, long>(_factory.CatalogDataMock, () => metadata);

        var response = await _client.GetAsync($"/playlist/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Playlist_With_Invalid_Model_Returns_400()
    {
        var metadata = CatalogMetadataHelper.InitTestObject();
        _factory.SetupPersistenceStoreMock<CatalogMetadata, Guid, long>(_factory.CatalogDataMock, () => metadata);

        var dto = new PublicMetadataDto
        {
            Name = "Test",
            Description = GenerateString501(),
            Type = "playlist"
        };

        var response = await _client.PostAsJsonAsync("/playlist", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Playlist_With_Invalid_Model_Returns_404()
    {
        var metadata = CatalogMetadataHelper.InitTestObject();
        _factory.SetupPersistenceStoreMock<CatalogMetadata, Guid, long>(_factory.CatalogDataMock, () => metadata);

        var dto = new PublicMetadataDto
        {
            Name = "",
            Type = "playlist"
        };

        var response = await _client.PatchAsJsonAsync($"/playlist/{Guid.NewGuid()}", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Playlist_Returns_204()
    {
        var metadata = CatalogMetadataHelper.InitTestObject();
        _factory.SetupPersistenceStoreMock<CatalogMetadata, Guid, long>(_factory.CatalogDataMock, () => metadata);

        var response = await _client.DeleteAsync($"/playlist/{CatalogMetadataHelper.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Playlist_Returns_404()
    {
        var metadata = CatalogMetadataHelper.InitTestObject();
        _factory.SetupPersistenceStoreMock<CatalogMetadata, Guid, long>(_factory.CatalogDataMock, () => metadata);

        var response = await _client.DeleteAsync($"/playlist/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string GenerateString501()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ";
        var result = new char[501];
        Span<byte> bytes = stackalloc byte[501];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        for (int i = 0; i < result.Length; i++) result[i] = chars[bytes[i] % chars.Length];
        return new string(result);
    }
}