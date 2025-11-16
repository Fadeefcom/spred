using System.Text.Json;
using AutoMapper;
using FluentAssertions;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;
using AggregatorService.Configurations;
using AggregatorService.Models;

namespace AggregatorService.Test.Configurations;

public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void MappingProfile_Should_BeValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void MapDictUrls_Should_Return_ValidDictionary()
    {
        var json = """{ "spotify": "https://spotify.com", "apple": "https://apple.com" }""";
        var element = JsonDocument.Parse(json).RootElement;
        var result = MappingProfile.MapDictUrls(element);
        result.Should().ContainKeys("spotify", "apple");
        result["apple"].Should().Be("https://apple.com");
    }

    [Fact]
    public void MapDictUrls_Should_Return_Empty_When_NotObject()
    {
        var json = """["a", "b"]""";
        var element = JsonDocument.Parse(json).RootElement;
        MappingProfile.MapDictUrls(element).Should().BeEmpty();
    }

    [Fact]
    public void GetImageUrl_Should_Select_MaxHeight()
    {
        var json = """
        {
          "images": [
            { "url": "http://small", "height": 200 },
            { "url": "http://max", "height": 640 },
            { "url": "http://mid", "height": 400 }
          ]
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;
        MappingProfile.GetImageUrl(element).Should().Be("http://max");
    }

    [Fact]
    public void GetImageUrl_Should_Fallback_When_NoHeight()
    {
        var json = """{ "images": [{ "url": "http://fallback" }] }""";
        var element = JsonDocument.Parse(json).RootElement;
        MappingProfile.GetImageUrl(element).Should().Be("http://fallback");
    }

    [Fact]
    public void GetImageUrl_Should_Return_Empty_When_NoImages()
    {
        var json = """{ "images": [] }""";
        var element = JsonDocument.Parse(json).RootElement;
        MappingProfile.GetImageUrl(element).Should().BeEmpty();
    }

    [Fact]
    public void GetImageUrl_Should_Return_Empty_When_NoField()
    {
        var json = """{ "name": "x" }""";
        var element = JsonDocument.Parse(json).RootElement;
        MappingProfile.GetImageUrl(element).Should().BeEmpty();
    }

    [Theory]
    [InlineData("spotify", "123", "https://open.spotify.com/playlist/123")]
    [InlineData("apple", "999", "https://music.apple.com/playlist/999")]
    [InlineData("deezer", "x", "https://www.deezer.com/playlist/x")]
    [InlineData("youtube", "YID", "https://music.youtube.com/playlist?list=YID")]
    [InlineData("soundcloud", "gleb", "https://soundcloud.com/gleb")]
    [InlineData("unknown", "id", "")]
    public void BuildPlatformPlaylistUrl_Should_Build_Expected(string platform, string id, string expected)
    {
        var result = MappingProfile.BuildPlatformPlaylistUrl(platform, id);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("spotify", "abc", "https://open.spotify.com/track/abc")]
    [InlineData("apple", "zzz", "https://music.apple.com/track/zzz")]
    [InlineData("deezer", "x", "https://www.deezer.com/track/x")]
    [InlineData("youtube", "QID", "https://music.youtube.com/watch?v=QID")]
    [InlineData("soundcloud", "gleb", "https://soundcloud.com/gleb")]
    [InlineData("unknown", "id", "")]
    public void BuildPlatformTrackUrl_Should_Build_Expected(string platform, string id, string expected)
    {
        var result = MappingProfile.BuildPlatformTrackUrl(platform, id);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task Should_Map_MetadataDto_From_Chartmetric_Json()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "TestData", "charmetric_test_catalog.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);
        var element = doc.RootElement.GetProperty("obj");

        var result = _mapper.Map<MetadataDto>(element);

        result.Should().NotBeNull();
        result.ChartmetricsId.Should().Be("9136486");
        result.Description.Should().Contain("Weekly Updated");
        result.ImageUrl.Should().Contain("ab67706c0000da843e03ba1ce1dcaa4a3147fabb");
        result.Followers.Should().Be(25102);
        result.TracksTotal.Should().Be(78);
        result.Tags.Should().Contain("Hip-Hop/Rap");
        result.Moods.Should().ContainKey(1);
        result.Activities.Should().ContainKey(1);
        result.PrimaryId.Should().StartWith("playlist:");
        result.ListenUrls.Should().ContainKey("spotify");
    }

    [Fact]
    public async Task Should_Map_TrackDto_From_ChartmetricsTrackWrapper()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "chartmetrics_track.json");
        var json = await File.ReadAllTextAsync(path);
        using var doc = JsonDocument.Parse(json);
        var array = doc.RootElement.GetProperty("obj").EnumerateArray().ToList();
        var wrappers = array.Select(el => new ChartmetricsTrackWrapper { Data = el }).ToList();

        var result = _mapper.Map<List<TrackDto>>(wrappers);
        result.Should().NotBeNullOrEmpty();

        var track = result.First();
        track.PrimaryId.Should().StartWith("track:");
        track.Title.Should().NotBeEmpty();
        track.SourceType.Should().Be(SourceType.ChartMetrics);
        track.Album.Should().NotBeNull();
        track.Artists.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Should_Map_StatInfo_From_Json()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "charmetrics_stats.json");
        var json = await File.ReadAllTextAsync(path);
        using var doc = JsonDocument.Parse(json);
        var array = doc.RootElement.GetProperty("obj");

        var result = _mapper.Map<HashSet<StatInfo>>(array.EnumerateArray().ToList());

        result.Should().NotBeEmpty();
        var stat = result.First();
        stat.Value.Should().BeGreaterThan(0);
        stat.Timestamp.Should().BeAfter(new DateTime(2023, 1, 1));
    }

    [Fact]
    public async Task Should_Map_MetadataTracksDto_From_SpotifyPlaylist()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "spotify_playlist.json");
        var json = await File.ReadAllTextAsync(path);
        using var doc = JsonDocument.Parse(json);

        var result = _mapper.Map<MetadataTracksDto>(doc.RootElement);

        result.Should().NotBeNull();
        result.PrimaryId.Should().StartWith("spotify:playlist:");
        result.Tracks.Should().NotBeEmpty();
        result.Status.Should().Be(FetchStatus.FetchedPlaylist);
        result.ListenUrls.Should().ContainKey("spotify");

        var track = result.Tracks.First();
        track.Album.Should().NotBeNull();
        track.Artists.Should().NotBeNullOrEmpty();
        track.SourceType.Should().Be(SourceType.Spotify);
        track.Title.Should().NotBeEmpty();
        track.Album.ImageUrl.Should().Contain("https://");
    }

    [Fact]
    public void SpotifyTrackWrapper_Should_Set_UpdateAt_To_UtcNow()
    {
        var json = """
        {
          "track": {
            "id": "abc123",
            "name": "Test Track",
            "album": {
              "id": "alb123",
              "name": "Album",
              "release_date": "2023-02-01",
              "images": [{ "url": "http://img.com", "height": 640 }]
            },
            "popularity": "42",
            "external_urls": { "spotify": "http://spotify.com/track/abc123" },
            "artists": [{ "id": "a1", "name": "Artist" }]
          },
          "added_at": "2023-02-02T00:00:00Z"
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;
        var wrapper = new SpotifyTrackWrapper { Data = element };

        var result = _mapper.Map<TrackDto>(wrapper);
        result.UpdateAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Album!.PrimaryId.Should().Be("spotify:album:alb123");
    }

    [Fact]
    public void Should_Map_SoundchartsPlaylistWrapper()
    {
        var json = """
        {
          "object": {
            "uuid": "xyz",
            "name": "Cool Playlist",
            "platform": "spotify",
            "identifier": "123",
            "owner": { "name": "Gleb", "identifier": "gleb-id" },
            "imageUrl": "https://cdn/img.png",
            "latestSubscriberCount": 42,
            "latestTrackCount": 8,
            "type": "playlist"
          }
        }
        """;
        var element = JsonDocument.Parse(json).RootElement;
        var wrapper = new SoundchartsPlaylistWrapper { Data = element };

        var result = _mapper.Map<MetadataDto>(wrapper);
        result.Should().NotBeNull();
        result.SoundChartsId.Should().Be("xyz");
        result.Followers.Should().Be(42);
        result.TracksTotal.Should().Be(8);
        result.ListenUrls.Should().ContainKey("spotify");
        result.ImageUrl.Should().Be("https://cdn/img.png");
        result.Name.Should().Be("Cool Playlist");
    }

    [Fact]
    public void Should_Map_SoundchartsTrackWrapper()
    {
        var json = """
        {
          "object": {
            "uuid": "trk123",
            "name": "Energy Flow",
            "platform": "spotify",
            "duration": 240,
            "releaseDate": "2023-01-10",
            "genres": [{ "root": "Electronic" }],
            "audio": { "tempo": 120, "energy": 0.8 },
            "artists": [
              { "uuid": "art1", "name": "Artist1", "imageUrl": "http://img1" }
            ],
            "imageUrl": "http://img-track"
          }
        }
        """;

        var element = JsonDocument.Parse(json).RootElement;
        var wrapper = new SoundchartsTrackWrapper { Data = element };

        var result = _mapper.Map<TrackDtoWithPlatformIds>(wrapper);

        result.Should().NotBeNull();
        result.Title.Should().Be("Energy Flow");
        result.Album.Should().NotBeNull();
        result.Album.ImageUrl.Should().Be("http://img-track");
        result.Artists.Should().ContainSingle(a => a.Name == "Artist1");
        result.Audio.Should().NotBeNull();
        result.Audio.Tempo.Should().Be(120);
        result.SourceType.Should().Be(SourceType.SoundCharts);
    }
}
