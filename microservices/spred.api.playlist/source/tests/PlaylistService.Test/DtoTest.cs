using PlaylistService.Models.DTO;
using PlaylistService.Models.Queries;

namespace PlaylistService.Test;

public class ModelsTests
{
    [Fact]
    public void Constructor_ShouldAssignPropertiesCorrectly()
    {
        var input = new Dictionary<Guid, Guid>
        {
            [Guid.NewGuid()] = Guid.NewGuid()
        };

        var query = new GetMetadataQueryByIds
        {
            OwnerMetadataIds = input,
            Type = "playlist"
        };

        Assert.Equal(input, query.OwnerMetadataIds);
        Assert.Equal("playlist", query.Type);
    }

    [Fact]
    public void Records_ShouldSupportEquality()
    {
        var ownerIds = new Dictionary<Guid, Guid>
        {
            [Guid.NewGuid()] = Guid.NewGuid()
        };

        var a = new GetMetadataQueryByIds { OwnerMetadataIds = ownerIds, Type = "playlist" };
        var b = new GetMetadataQueryByIds { OwnerMetadataIds = ownerIds, Type = "playlist" };

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Records_ShouldSupportWithExpression()
    {
        var original = new GetMetadataQueryByIds
        {
            OwnerMetadataIds = new Dictionary<Guid, Guid> { [Guid.NewGuid()] = Guid.NewGuid() },
            Type = "playlist"
        };

        var modified = original with { Type = "label" };

        Assert.Equal(original.OwnerMetadataIds, modified.OwnerMetadataIds);
        Assert.Equal("label", modified.Type);
        Assert.NotEqual(original, modified);
    }
}

public class PrivateMetadataDtoTests
{
    [Fact]
    public void Default_Collections_Should_NotBeNull_And_Empty()
    {
        var dto = new PrivateMetadataDto();
        
        Assert.NotNull(dto.ListenUrls);
        Assert.Empty(dto.ListenUrls);
        
        Assert.NotNull(dto.SubmitUrls);
        Assert.Empty(dto.SubmitUrls);
        
        Assert.NotNull(dto.Tags);
        Assert.Empty(dto.Tags);
    }

    [Fact]
    public void Init_Properties_Should_Be_Settable_Via_ObjectInitializer()
    {
        var id = Guid.NewGuid();
        var dto = new PrivateMetadataDto
        {
            Id = id,
            Name = "Indie Rock",
            Description = "Best indie playlists",
            ListenUrls = new Dictionary<string, string> { ["spotify"] = "https://spotify.com/x" },
            SubmitUrls = new Dictionary<string, string> { ["email"] = "mailto:test@example.com" },
            Tags = new List<string> { "indie", "rock" },
            Href = "https://spred.io/playlist/123",
            ImageUrl = "https://img.io/x.png",
            TracksTotal = 120,
            Followers = 5000,
            FollowerChange = 150,
            IsPublic = true,
            Collaborative = false,
            SubmitEmail = "submit@example.com",
            UpdatedAt = new DateTime(2025, 10, 8),
            Type = "Editorial",
            Platform = "Spotify"
        };

        Assert.Equal(id, dto.Id);
        Assert.Equal("Indie Rock", dto.Name);
        Assert.Equal("Best indie playlists", dto.Description);
        Assert.Equal("https://spotify.com/x", dto.ListenUrls["spotify"]);
        Assert.Equal("mailto:test@example.com", dto.SubmitUrls["email"]);
        Assert.Contains("indie", dto.Tags);
        Assert.Equal("https://spred.io/playlist/123", dto.Href);
        Assert.Equal("https://img.io/x.png", dto.ImageUrl);
        Assert.Equal((uint)120, dto.TracksTotal);
        Assert.Equal((uint)5000, dto.Followers);
        Assert.Equal(150, dto.FollowerChange);
        Assert.True(dto.IsPublic);
        Assert.False(dto.Collaborative);
        Assert.Equal("submit@example.com", dto.SubmitEmail);
        Assert.Equal(new DateTime(2025, 10, 8), dto.UpdatedAt);
        Assert.Equal("Editorial", dto.Type);
        Assert.Equal("Spotify", dto.Platform);
    }

    [Fact]
    public void Can_Modify_Mutable_Properties()
    {
        var dto = new PrivateMetadataDto { Followers = 10, Platform = "Apple Music" };

        dto.Followers = 20;
        dto.Platform = "YouTube";

        Assert.Equal((uint)20, dto.Followers);
        Assert.Equal("YouTube", dto.Platform);
    }

    [Fact]
    public void FollowerChange_Should_Allow_Negative_And_Positive_Values()
    {
        var dto = new PrivateMetadataDto { FollowerChange = -25 };
        Assert.Equal(-25, dto.FollowerChange);
        
        dto.FollowerChange = 100;
        Assert.Equal(100, dto.FollowerChange);
    }
}