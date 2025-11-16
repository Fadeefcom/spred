using Spred.Bus.DTOs;

namespace PlaylistService.Test;

public class ArtistDtoTests
{
    [Fact]
    public void ArtistDto_Should_Set_And_Get_Properties()
    {
        // Arrange
        var artist = new ArtistDto
        {
            PrimaryId = "artist:123",
            Name = "Test Artist",
            ImageUrl = "http://example.com/image.jpg"
        };

        // Assert
        Assert.Equal("artist:123", artist.PrimaryId);
        Assert.Equal("Test Artist", artist.Name);
        Assert.Equal("http://example.com/image.jpg", artist.ImageUrl);
    }

    [Fact]
    public void ArtistDto_Should_Have_Empty_PrimaryId_By_Default()
    {
        // Arrange
        var artist = new ArtistDto();

        // Assert
        Assert.Equal(string.Empty, artist.PrimaryId);
        Assert.Empty(artist.Name);
        Assert.Empty(artist.ImageUrl);
    }
}