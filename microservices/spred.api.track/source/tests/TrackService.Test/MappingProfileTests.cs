using AutoMapper;
using FluentAssertions;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;
using TrackService.Configuration;
using TrackService.Models.Commands;
using TrackService.Models.DTOs;
using TrackService.Models.Entities;

namespace TrackService.Test;

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
    public void Configuration_IsValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Should_Map_TrackDto_To_TrackMetadata_AndBack()
    {
        // Arrange
        var dto = new TrackDto
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            PrimaryId = "pid-123",
            Title = "My Track",
            Description = "Description text",
            ChartmetricsId = "chart-001",
            SoundChartsId = "sound-001",
            ImageUrl = "https://cdn/image.jpg",
            LanguageCode = "en",
            Popularity = 95,
            SourceType = SourceType.Spotify,
            Published = DateTime.UtcNow,
            AddedAt = DateTime.UtcNow.AddDays(-2),
            UpdateAt = DateTime.UtcNow.AddDays(-1),
            Album = new AlbumDto
            {
                AlbumName = "Album 1",
                AlbumLabel = "Label",
                AlbumReleaseDate = "2024-01-01",
                ImageUrl = "https://cdn/album.jpg",
                PrimaryId = "alb-1"
            },
            Artists = new List<ArtistDto>
            {
                new() { Name = "Artist A", PrimaryId = "art-1", ImageUrl = "https://cdn/artist.jpg" }
            },
            Audio = new AudioFeaturesDto
            {
                Bitrate = 320,
                SampleRate = 44100,
                Channels = 2,
                Codec = "mp3",
                Bpm = 128,
                Genre = "Electronic",
                Energy = 0.8,
                Valence = 0.7,
                Duration = TimeSpan.FromMinutes(3)
            },
            TrackUrl = new List<PlatformUrl>
            {
                new() { Platform = "Spotify", Value = new Uri("https://spotify.com/track") }
            }
        };

        // Act
        var entity = _mapper.Map<TrackMetadata>(dto);
        var mappedBack = _mapper.Map<TrackDto>(entity);

        // Assert
        entity.Should().NotBeNull();
        entity.Title.Should().Be(dto.Title);
        entity.Audio.Genre.Should().Be(dto.Audio.Genre);
        //entity.SpredUserId.Should().Be(dto.OwnerId);

        mappedBack.Should().BeEquivalentTo(dto, opt =>
            opt.Excluding(x => x.OwnerId)
                .Excluding(x => x.PrimaryId)
                .Excluding(x => x.ImageUrl)); // ImageUrl может браться из Album
    }

    [Fact]
    public void Should_Map_TrackMetadata_To_PublicTrackDto()
    {
        // Arrange
        var entity = new TrackMetadata();
        var now = DateTime.UtcNow;

        typeof(TrackMetadata).GetProperty(nameof(TrackMetadata.Title))!.SetValue(entity, "Public Title");
        typeof(TrackMetadata).GetProperty(nameof(TrackMetadata.Description))!.SetValue(entity, "Desc");
        typeof(TrackMetadata).GetProperty(nameof(TrackMetadata.ImageUrl))!.SetValue(entity, "https://cdn/img.jpg");
        typeof(TrackMetadata).GetProperty(nameof(TrackMetadata.Published))!.SetValue(entity, now);
        typeof(TrackMetadata).GetProperty(nameof(TrackMetadata.Artists))!.SetValue(entity,
            new List<Artist> { new() { Name = "Artist 1", PrimaryId = "a1" } });
        typeof(TrackMetadata).GetProperty(nameof(TrackMetadata.Album))!.SetValue(entity,
            new Album { PrimaryId = "test", AlbumName = "Alb", ImageUrl = "https://cdn/album.jpg" });

        // Act
        var result = _mapper.Map<PublicTrackDto>(entity);

        // Assert
        result.Title.Should().Be("Public Title");
        result.Artists.Should().ContainSingle(x => x.Name == "Artist 1");
    }

    [Fact]
    public void Should_Map_AlbumDto_To_Album_AndBack()
    {
        // Arrange
        var dto = new AlbumDto
        {
            AlbumName = "album",
            AlbumLabel = "label",
            AlbumReleaseDate = "2024-02-01",
            ImageUrl = "https://cdn/a.jpg",
            PrimaryId = "alb-2"
        };

        // Act
        var entity = _mapper.Map<Album>(dto);
        var mappedBack = _mapper.Map<AlbumDto>(entity);

        // Assert
        mappedBack.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void Should_Map_ArtistDto_To_Artist_AndBack()
    {
        var dto = new ArtistDto
        {
            Name = "John Doe",
            PrimaryId = "art-001",
            ImageUrl = "https://cdn/img.png"
        };

        var entity = _mapper.Map<Artist>(dto);
        var mappedBack = _mapper.Map<ArtistDto>(entity);

        mappedBack.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public void Should_Map_TrackMetadata_To_PrivateTrackDto()
    {
        // Arrange
        var entity = new TrackMetadata();
        var id = Guid.NewGuid();

        typeof(TrackMetadata).GetProperty(nameof(TrackMetadata.Id))!.SetValue(entity, id);
        typeof(TrackMetadata).GetProperty(nameof(TrackMetadata.Audio))!.SetValue(entity, new AudioFeatures
        {
            Duration = TimeSpan.FromSeconds(180),
            Bitrate = 320,
            SampleRate = 44100,
            Channels = 2,
            Codec = "mp3",
            Bpm = 128,
            Genre = "HipHop",
            Energy = 0.8,
            Valence = 0.6
        });

        typeof(TrackMetadata).GetProperty(nameof(TrackMetadata.Artists))!.SetValue(entity,
            new List<Artist> { new() { PrimaryId = "test", Name = "Artist 1", ImageUrl = "https://cdn/a.png" } });
        typeof(TrackMetadata).GetProperty(nameof(TrackMetadata.Album))!.SetValue(entity,
            new Album { PrimaryId = "test", AlbumName = "Alb", ImageUrl = "https://cdn/alb.jpg" });
        typeof(TrackMetadata).GetProperty(nameof(TrackMetadata.TrackLinks))!.SetValue(entity,
            new List<TrackLink> { new() { Platform = "Spotify", Value = "https://spotify.com/track" } });

        // Act
        var dto = _mapper.Map<PrivateTrackDto>(entity);

        // Assert
        dto.Id.Should().Be(id.ToString());
        dto.Genre.Should().Be("HipHop");
        dto.Album.Should().NotBeNull();
        dto.Artists.Should().ContainSingle(a => a.Name == "Artist 1");
        dto.TrackUrl.Should().ContainSingle(u => u.Platform == "Spotify");
    }

    [Fact]
    public void Should_Map_PrivateArtist_And_PrivateAlbum_Directly()
    {
        // Arrange
        var artist = new Artist { PrimaryId = "test", Name = "Test Artist", ImageUrl = "https://cdn/a.jpg" };
        var album = new Album
        {
            PrimaryId = "test",
            AlbumName = "Album",
            AlbumLabel = "Label",
            AlbumReleaseDate = "2024-02-01",
            ImageUrl = "https://cdn/b.jpg"
        };

        // Act
        var artistDto = _mapper.Map<PrivateArtistDto>(artist);
        var albumDto = _mapper.Map<PrivateAlbumDto>(album);

        // Assert
        artistDto.Name.Should().Be("Test Artist");
        albumDto.AlbumName.Should().Be("Album");
        albumDto.ImageUrl.Should().Be("https://cdn/b.jpg");
    }

    [Fact]
    public void Should_Map_TrackCreate_To_TrackDto()
    {
        // Arrange
        var create = new TrackCreate
        {
            Title = "Track title",
            Description = "Desc",
            TrackUrl = "url"
        };

        // Act
        var dto = _mapper.Map<TrackDto>(create);

        // Assert
        dto.Should().NotBeNull();
    }

    [Fact]
    public void Should_Map_CreateTrackMetadataItemCommand_To_UpdateTrackMetadataItemCommand()
    {
        // Arrange
        var command = new CreateTrackMetadataItemCommand(
            new TrackDtoWithPlatformIds
            {
                Title = "Test Track",
                Description = "Demo description",
                ImageUrl = "https://image.jpg",
                LanguageCode = "en",
                ChartmetricsId = "cm123",
                SoundChartsId = "sc456",
                Popularity = 77,
                Published = DateTime.UtcNow.AddDays(-5),
                AddedAt = DateTime.UtcNow.AddDays(-6),
                UpdateAt = DateTime.UtcNow,
                SourceType = SourceType.Direct,
                Audio = new AudioFeaturesDto
                {
                    Bitrate = 320,
                    Bpm = 120,
                    Channels = 2,
                    Codec = "mp3",
                    Duration = TimeSpan.FromSeconds(180),
                    SampleRate = 44100
                },
                Artists = new List<ArtistDto>
                {
                    new() { Name = "Artist", PrimaryId = "a123", ImageUrl = "https://artist.jpg" }
                },
                Album = new AlbumDto
                {
                    AlbumName = "Album",
                    PrimaryId = "alb123",
                    AlbumLabel = "Label",
                    AlbumReleaseDate = DateTime.UtcNow.AddYears(-1).ToString()
                },
                TrackUrl = new List<PlatformUrl>
                {
                    new() { Platform = "spotify", Value = new Uri("https://spotify.com/t1") }
                },
                PrimaryIds = new List<PlatformIdPair> { new("spotify", "t1") }
            },
            Guid.NewGuid(),
            null
        );

        // Act
        var result = _mapper.Map<UpdateTrackMetadataItemCommand>(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.Title, result.Title);
        Assert.Equal(command.Description, result.Description);
        Assert.Equal(command.ImageUrl, result.ImageUrl);
        Assert.Equal(command.LanguageCode, result.LanguageCode);
        Assert.Equal(command.ChartmetricsId, result.ChartmetricsId);
        Assert.Equal(command.SoundChartsId, result.SoundChartsId);
        Assert.Equal(command.Audio.Bitrate, result.Audio?.Bitrate);
        Assert.Equal(command.Audio.Codec, result.Audio?.Codec);
        Assert.Equal(command.Popularity, result.Popularity);
        Assert.Equal(command.SourceType, result.SourceType);
        Assert.NotNull(result.Artists);
        Assert.NotEmpty(result.UpdatedTrackLinks);
    }
    
    [Fact]
    public void Should_Map_TrackCreate_To_TrackDto_Correctly()
    {
        var source = new TrackCreate
        {
            Title = "Test Track",
            Description = "Test description",
            TrackUrl = "https://example.com/track"
        };

        var result = _mapper.Map<TrackDto>(source);

        Assert.Equal(source.Title, result.Title);
        Assert.Equal(source.Description, result.Description);
    }
}
