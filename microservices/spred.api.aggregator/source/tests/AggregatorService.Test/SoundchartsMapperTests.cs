using System.Text.Json;
using AggregatorService.Components;
using AggregatorService.Models;
using AutoMapper;
using FluentAssertions;
using Spred.Bus.DTOs;

namespace AggregatorService.Test;

public class SoundchartsMapperTests
{
    private readonly IMapper _mapper;

    public SoundchartsMapperTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(SoundchartsCatalogProvider).Assembly);
        });

        _mapper = config.CreateMapper();
        config.AssertConfigurationIsValid();
    }

    private static JsonElement LoadJson(string fileName)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    [Theory]
    [InlineData("soundcharts_track.json", "TrackDtoWithPlatformIds")]
    [InlineData("soundcharts_playlist.json", "MetadataDto")]
    public void Mapper_Should_Map_All_Files_Correctly(string file, string expectedType)
    {
        var data = LoadJson(file);

        if (expectedType == "TrackDtoWithPlatformIds")
        {
            var wrapper = new SoundchartsTrackWrapper { Data = data};
            var dto = _mapper.Map<TrackDtoWithPlatformIds>(wrapper);

            dto.Should().NotBeNull();
            dto.Title.Should().NotBeNullOrEmpty();
            dto.Artists.Should().NotBeEmpty();
            dto.Audio.Should().NotBeNull();
        }

        if (expectedType == "MetadataDto")
        {
            var wrapper = new SoundchartsPlaylistWrapper { Data = data };
            var dto = _mapper.Map<MetadataDto>(wrapper);

            dto.Should().NotBeNull();
            dto.PrimaryId.Should().NotBeNullOrEmpty();
            dto.Name.Should().NotBeNullOrEmpty();
            dto.OwnerPlatformId.Should().NotBeNullOrEmpty();
            dto.OwnerPlatformName.Should().NotBeNullOrEmpty();
            dto.UserPlatformId.Should().NotBeNullOrEmpty();
        }
    }
}