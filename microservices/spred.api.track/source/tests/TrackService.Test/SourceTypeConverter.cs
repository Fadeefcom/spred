using System.Text.Json;
using Spred.Bus.DTOs;
using TrackService.Helpers;

namespace TrackService.Test;

public class SourceTypeConverterTests
{
    private readonly JsonSerializerOptions _options;

    public SourceTypeConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new SourceTypeConverter() }
        };
    }

    [Theory]
    [InlineData(SourceType.Direct)]
    [InlineData(SourceType.Spotify)]
    [InlineData(SourceType.SoundCharts)]
    [InlineData(SourceType.ChartMetrics)]
    public void Write_ShouldSerializeEnumAsString(SourceType type)
    {
        // Act
        var json = JsonSerializer.Serialize(type, _options);

        // Assert
        Assert.Equal($"\"{type}\"", json);
    }

    [Theory]
    [InlineData("\"Direct\"", SourceType.Direct)]
    [InlineData("\"Spotify\"", SourceType.Spotify)]
    [InlineData("\"SoundCharts\"", SourceType.SoundCharts)]
    [InlineData("\"ChartMetrics\"", SourceType.ChartMetrics)]
    public void Read_ShouldDeserializeFromString(string json, SourceType expected)
    {
        // Act
        var result = JsonSerializer.Deserialize<SourceType>(json, _options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("0", SourceType.Direct)]
    [InlineData("1", SourceType.Spotify)]
    [InlineData("2", SourceType.SoundCharts)]
    [InlineData("3", SourceType.ChartMetrics)]
    public void Read_ShouldDeserializeFromInt(string json, SourceType expected)
    {
        // Act
        var result = JsonSerializer.Deserialize<SourceType>(json, _options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("\"invalid-string\"")]
    [InlineData("123")]
    public void Read_ShouldThrowJsonException_OnInvalidValue(string json)
    {
        // Act + Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<SourceType>(json, _options));

        Assert.NotNull(ex.Message);
    }

    [Fact]
    public void Read_ShouldThrowJsonException_OnInvalidToken()
    {
        // Arrange
        var json = "true"; // unexpected token

        // Act + Assert
        var ex = Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<SourceType>(json, _options));

        Assert.Contains("Unexpected token", ex.Message);
    }
}