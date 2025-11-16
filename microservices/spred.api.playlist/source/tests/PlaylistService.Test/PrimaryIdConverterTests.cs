using Newtonsoft.Json;
using PlaylistService.Configuration;
using PlaylistService.Models;

namespace PlaylistService.Test;

public class PrimaryIdConverterTests
{
    private readonly JsonSerializerSettings _settings;

    public PrimaryIdConverterTests()
    {
        _settings = new JsonSerializerSettings
        {
            Converters = { new PrimaryIdConverter() }
        };
    }

    [Fact]
    public void WriteJson_ShouldSerializePrimaryId_ToStringValue()
    {
        var id = PrimaryId.Parse("spotify:track:12345");
        var json = JsonConvert.SerializeObject(id, _settings);
        Assert.Equal("\"spotify:track:12345\"", json);
    }

    [Fact]
    public void ReadJson_ShouldDeserializeValidString_ToPrimaryId()
    {
        var json = "\"spotify:track:12345\"";
        var result = JsonConvert.DeserializeObject<PrimaryId>(json, _settings);
        Assert.Equal("spotify:track:12345", result.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReadJson_ShouldReturnDefault_WhenStringIsNullOrWhitespace(string? value)
    {
        var json = JsonConvert.SerializeObject(value);
        var result = JsonConvert.DeserializeObject<PrimaryId>(json, _settings);
        Assert.Equal(default, result);
    }

    [Fact]
    public void WriteJson_ShouldWriteNull_WhenValueIsDefault()
    {
        var json = new PrimaryId("test", "test", "123");
        Assert.Equal("test:test:123", json.ToString());;
    }

    [Fact]
    public void ReadJson_ShouldHandleUnexpectedType_Gracefully()
    {
        var json = "test:test:123";
        var result = PrimaryId.Parse(json);
        Assert.Equal(json, result.ToString());
    }
}