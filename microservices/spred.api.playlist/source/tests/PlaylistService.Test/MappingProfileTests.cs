using AutoMapper;
using FluentAssertions;
using PlaylistService.Configuration;
using PlaylistService.Models;
using PlaylistService.Models.Commands;
using PlaylistService.Models.DTO;
using PlaylistService.Models.Entities;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;

namespace PlaylistService.Test;

public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var config = new MapperConfiguration(cfg => { cfg.AddProfile<MappingProfile>(); });

        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void MappingConfiguration_IsValid()
    {
        Assert.True(true);
    }

    [Fact]
    public void MetadataDto_MapsTo_CatalogMetadata_AndBack()
    {
        var dto = new MetadataDto
        {
            Id = Guid.NewGuid(),
            PrimaryId = "test:test:playlist-123",
            Name = "Test Playlist",
            Description = "A test playlist",
            ListenUrls = new Dictionary<string, string> { ["spotify"] = "https://open.spotify.com" },
            SubmitUrls = new Dictionary<string, string> { ["form"] = "https://submit.com" },
            ImageUrl = "https://image.url",
            TracksTotal = 42,
            Followers = 100,
            IsPublic = true,
            Collaborative = true,
            Tracks = new List<Guid> { },
            SubmitEmail = "test@example.com",
            SpredUserId = Guid.NewGuid(),
            Type = "playlist",
            Href = "",
            CatalogType = "Test"
        };

        var catalog = _mapper.Map<CatalogMetadata>(dto);
        var result = _mapper.Map<MetadataDto>(catalog);

        result.Should().BeEquivalentTo(dto, options =>
            options
                .Excluding(x => x.SpredUserId)
                .Excluding(x => x.Type));
    }

    [Theory]
    [InlineData("playlistMetadata", "playlist")]
    [InlineData("recordLabelMetadata", "record_label")]
    public void CatalogMetadata_MapsTo_PublicMetadataDto_WithType(string inputType, string expected)
    {
        var entity = new CatalogMetadata();
        typeof(CatalogMetadata).GetProperty("Type")!.SetValue(entity, inputType);
        typeof(CatalogMetadata).GetProperty("PrimaryId")!.SetValue(entity, PrimaryId.Parse("spotify:playlist:1234567890"));

        var result = _mapper.Map<PublicMetadataDto>(entity);
        result.Type.Should().Be(expected);
        result.Platform.Should().Be("spotify");
    }

    [Fact]
    public void MetadataDto_MapsTo_CreateMetadataCommand()
    {
        var command = new MetadataDto
        {
            Id = Guid.NewGuid(),
            PrimaryId = "test:test:test",
            Name = "Name",
            Description = "Desc",
            IsPublic = true,
            Collaborative = true,
            Followers = 10,
            Tracks = [],
            ListenUrls = new() { ["s"] = "url" },
            SubmitUrls = new() { ["s"] = "url" },
            SubmitEmail = "email@x.com",
            ImageUrl = "img",
            SpredUserId = Guid.NewGuid(),
            TracksTotal = 0,
            Href = ""
        };

        var back = _mapper.Map<CreateMetadataCommand>(command);

        back.Should().BeEquivalentTo(command, options => options.Excluding(m => m.PrimaryId).ExcludingMissingMembers());
        back.PrimaryId.ToString().Should().Be(command.PrimaryId);
    }

    [Fact]
    public void MetadataDto_MapsTo_UpdateMetadataCommand()
    {
        var cmd = new MetadataDto
        {
            Id = Guid.NewGuid(),
            PrimaryId = "test:test:test",
            Name = "Name",
            Description = "Desc",
            IsPublic = true,
            Collaborative = true,
            Followers = 5,
            Tracks = [],
            ListenUrls = new() { ["s"] = "url" },
            SubmitUrls = new() { ["s"] = "url" },
            SubmitEmail = "email@x.com",
            ImageUrl = "img",
            SpredUserId = Guid.NewGuid(),
            ChartmetricsId = "chart",
            CatalogType = "Frontline",
            ActiveRatio = 0.7,
            SuspicionScore = 0.3,
            Moods = new() { [0] = "Happy" },
            Activities = new() { [0] = "Gym" },
            Tags = new() { "tag1", "tag2" },
            TracksTotal = 20
        };

        var dto = _mapper.Map<UpdateMetadataCommand>(cmd);

        dto.Should().BeEquivalentTo(cmd, options => options
            .ExcludingMissingMembers()
            .Excluding(x => x.Type)
            .Excluding(x => x.Href)
            .Excluding(x => x.Status)
            .Excluding(m => m.PrimaryId));
        dto.TracksTotal.Should().Be(cmd.TracksTotal);
        
        dto.PrimaryId.ToString().Should().Be(cmd.PrimaryId);
    }

    [Fact]
    public void MetadataDto_MapsTo_PublicMetadataDto()
    {
        var dto = new MetadataDto
        {
            Id = Guid.NewGuid(),
            PrimaryId = "playlist-123",
            Name = "Test",
            Description = "desc",
            IsPublic = true,
            Collaborative = false,
            Followers = 1,
            TracksTotal = 2,
            SubmitEmail = "x@x.com",
            ImageUrl = "img",
            ListenUrls = new() { ["s"] = "url" },
            SubmitUrls = new() { ["s"] = "url" }
        };

        var result = _mapper.Map<PublicMetadataDto>(dto);

        result.Should().BeEquivalentTo(dto, options => options.ExcludingMissingMembers());
    }

    [Fact]
    public void CatalogCreate_MapsTo_CreateMetadataCommand_AndUpdateMetadataCommand()
    {
        var catalogCreate = new CatalogCreate
        {
            Tracks = new List<TrackDto> { new TrackDto { Id = Guid.NewGuid(), Title = "track" } }
        };

        var createCommand = _mapper.Map<CreateMetadataCommand>(catalogCreate);
        createCommand.Tracks.Should().NotBeNull();

        var updateCommand = _mapper.Map<UpdateMetadataCommand>(catalogCreate);
        updateCommand.Tracks.Should().NotBeNull();
    }

    [Fact]
    public void MetadataTracksDto_MapsTo_UpdateMetadataCommand()
    {
        var trackId = Guid.NewGuid();
        var metadataTracks = new MetadataTracksDto
        {
            PrimaryId = "test:test:test",
            Tracks = new List<TrackDto> { new TrackDto { Id = trackId, Title = "track" } }
        };

        var updateCommand = _mapper.Map<UpdateMetadataCommand>(metadataTracks);
        updateCommand.Tracks.Should().Contain(trackId);
    }
}

