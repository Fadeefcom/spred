using Microsoft.AspNetCore.Http;
using Moq;
using Spred.Bus.DTOs;
using TrackService.Models.Commands;
using TrackService.Models.Entities;

namespace TrackService.Test;

public class TrackMetadataCommandTests
{
    [Fact]
    public void Update_ShouldApply_AllFields_FromCommand()
    {
        // Arrange
        var dto = new TrackDto
        {
            Id = Guid.NewGuid(),
            PrimaryId = "external-123",
            Title = "Updated Title",
            Description = "Updated Description",
            Audio = new AudioFeaturesDto
            {
                Codec = "aac",
                Genre = "Rock",
                Duration = TimeSpan.FromSeconds(200),
                Bitrate = 256000,
                SampleRate = 48000,
                Channels = 1,
                Bpm = 100
            },
            Popularity = 77,
            Published = DateTime.UtcNow
        };

        var id = dto.Id.Value;
        var spredUserId = Guid.NewGuid();
        var command = new UpdateTrackMetadataItemCommand(dto, id, spredUserId);
        var metadata = new TrackMetadata();

        // Act
        metadata.Update(command);

        // Assert
        Assert.Equal(dto.Title, metadata.Title);
        Assert.Equal(dto.Description, metadata.Description);
        Assert.Equal(dto.Audio.Codec, metadata.Audio.Codec);
        Assert.Equal(dto.Audio.Genre, metadata.Audio.Genre);
        Assert.Equal(dto.Audio.Duration, metadata.Audio.Duration);
        Assert.Equal(dto.Audio.Bitrate, metadata.Audio.Bitrate);
        Assert.Equal(dto.Audio.SampleRate, metadata.Audio.SampleRate);
        Assert.Equal(dto.Audio.Channels, metadata.Audio.Channels);
        Assert.Equal(dto.Audio.Bpm, metadata.Audio.Bpm);
        Assert.Equal(dto.Popularity, metadata.Popularity);
        Assert.Equal("", metadata.ContainerName);
    }

    [Fact]
    public void Create_ShouldSetAllFields_FromTrackDto()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TRACK_CONTAINER_NAME", "test-container");

        var dto = new TrackDto
        {
            Id = Guid.NewGuid(),
            PrimaryId = "external-456",
            Title = "New Title",
            Description = "New Description",
            Audio = new AudioFeaturesDto
            {
                Codec = "flac",
                Genre = "Jazz",
                Duration = TimeSpan.FromSeconds(150),
                Bitrate = 1411200,
                SampleRate = 96000,
                Channels = 2,
                Bpm = 85
            },
            Artists = [new ArtistDto { Name = "New Artist", PrimaryId = "empty" }],
            Album = new AlbumDto
            {
                AlbumName = "New Album",
                ImageUrl = "https://cdn/cover.jpg",
                PrimaryId = "empty",
                AlbumLabel = "",
                AlbumReleaseDate = ""
            },
            Published = DateTime.UtcNow.AddDays(-5),
            SourceType = SourceType.Direct,
            Popularity = 80
        };

        var spredUserId = Guid.NewGuid();
        var fakeFile = new Mock<IFormFile>().Object;

        var command = new CreateTrackMetadataItemCommand(dto, spredUserId, fakeFile);
        var metadata = new TrackMetadata();

        // Act
        metadata.Create(command);

        // Assert
        Assert.Equal(spredUserId, metadata.SpredUserId);
        Assert.Equal(dto.Title, metadata.Title);
        Assert.Equal(dto.Description, metadata.Description);

        // Audio assertions
        Assert.Equal(dto.Audio.Codec, metadata.Audio.Codec);
        Assert.Equal(dto.Audio.Genre, metadata.Audio.Genre);
        Assert.Equal(dto.Audio.Duration, metadata.Audio.Duration);
        Assert.Equal(dto.Audio.Bitrate, metadata.Audio.Bitrate);
        Assert.Equal(dto.Audio.SampleRate, metadata.Audio.SampleRate);
        Assert.Equal(dto.Audio.Channels, metadata.Audio.Channels);
        Assert.Equal(dto.Audio.Bpm, metadata.Audio.Bpm);

        // Structural assertions
        Assert.Equal("test-container", metadata.ContainerName);
        Assert.Equal(dto.Published, metadata.Published);
        Assert.Equivalent(dto.Artists, metadata.Artists);
        Assert.Equivalent(dto.Album, metadata.Album);
        Assert.Equal(dto.SourceType, metadata.SourceType);
        Assert.Equal(dto.Popularity, metadata.Popularity);
        Assert.Equal(UploadStatus.Pending, metadata.Status);
        Assert.False(metadata.IsDeleted);
    }

    [Fact]
    public void Delete_ShouldMarkEntityAsDeleted()
    {
        // Arrange
        var metadata = new TrackMetadata();

        // Act
        metadata.Delete();

        // Assert
        Assert.True(metadata.IsDeleted);
    }

    [Fact]
    public void StatusCreated_ShouldSetStatusToCreated()
    {
        // Arrange
        var metadata = new TrackMetadata();

        // Act
        metadata.StatusCreated();

        // Assert
        Assert.Equal(UploadStatus.Created, metadata.Status);
    }

    [Fact]
    public void ResetId_ShouldAssignNewGuid()
    {
        // Arrange
        var metadata = new TrackMetadata();
        var oldId = metadata.Id;

        // Act
        metadata.ResetId();

        // Assert
        Assert.NotEqual(oldId, metadata.Id);
        Assert.NotEqual(Guid.Empty, metadata.Id);
    }
}
