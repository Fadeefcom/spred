using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using TrackService.Models.Entities;

namespace TrackService.Test;

public class TrackPlatformIdTests
{
    [Fact]
    public void Constructor_Should_Initialize_Id()
    {
        var entity = new TrackPlatformId
        {
            SpredUserId = Guid.Empty,
            TrackMetadataId = Guid.NewGuid(),
            Platform = Platform.Spotify,
            PlatformTrackId = "abc123"
        };

        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void Properties_Should_Be_Assigned_Correctly()
    {
        var metadataId = Guid.NewGuid();
        var entity = new TrackPlatformId
        {
            SpredUserId = Guid.NewGuid(),
            TrackMetadataId = metadataId,
            Platform = Platform.YouTube,
            PlatformTrackId = "yt_789"
        };

        Assert.Equal(metadataId, entity.TrackMetadataId);
        Assert.Equal(Platform.YouTube, entity.Platform);
        Assert.Equal("yt_789", entity.PlatformTrackId);
    }

    [Fact]
    public void Id_Etag_Timestamp_Should_Be_Readonly()
    {
        var entity = new TrackPlatformId
        {
            SpredUserId = Guid.Empty,
            TrackMetadataId = Guid.NewGuid(),
            Platform = Platform.SoundCloud,
            PlatformTrackId = "sc_456"
        };

        var id = entity.Id;
        Assert.NotEqual(Guid.Empty, id);
        Assert.Null(entity.ETag);
        Assert.Equal(0, entity.Timestamp);
    }

    [Fact]
    public void PlatformTrackId_Should_Have_StringLength_Attribute()
    {
        var prop = typeof(TrackPlatformId).GetProperty(nameof(TrackPlatformId.PlatformTrackId))!;
        var attr = prop.GetCustomAttributes(typeof(StringLengthAttribute), false)
            .Cast<StringLengthAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attr);
        Assert.Equal(100, attr.MaximumLength);
    }

    [Fact]
    public void Platform_Should_Have_PartitionKey_Attribute()
    {
        var prop = typeof(TrackPlatformId).GetProperty(nameof(TrackPlatformId.Platform))!;
        var attr = prop.GetCustomAttributes(false)
            .FirstOrDefault(a => a.GetType().Name == "PartitionKeyAttribute");

        Assert.NotNull(attr);
    }

    [Fact]
    public void ToJson_And_FromJson_Should_Preserve_Platform_Name()
    {
        var platform = Platform.AppleMusic;
        var json = JsonConvert.SerializeObject(platform);
        var deserialized = JsonConvert.DeserializeObject<Platform>(json);

        Assert.Equal("\"AppleMusic\"", json);
        Assert.Equal(platform, deserialized);
    }

    [Fact]
    public void All_Platform_Enum_Members_Should_Be_Unique()
    {
        var values = Enum.GetValues(typeof(Platform)).Cast<int>().ToList();
        Assert.Equal(values.Count, values.Distinct().Count());
    }

    [Fact]
    public void Enum_Should_Contain_Known_Values()
    {
        var names = Enum.GetNames(typeof(Platform));
        Assert.Contains("Spotify", names);
        Assert.Contains("YouTubeMusic", names);
        Assert.Contains("AppleMusic", names);
        Assert.Contains("NetEase", names);
        Assert.Contains("Napster", names);
    }
}