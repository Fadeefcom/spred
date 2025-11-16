using PlaylistService.Models.Entities;

namespace PlaylistService.Test;

public class RecordLabelMetadataTests
{
    [Fact]
    public void Constructor_Should_Set_Type_To_RecordLabelMetadata()
    {
        // Act
        var metadata = new RecordLabelMetadata();

        // Assert
        Assert.Equal("record", metadata.Type);
    }

    [Fact]
    public void Should_Be_Assignable_To_CatalogMetadata()
    {
        // Act
        var metadata = new RecordLabelMetadata();

        // Assert
        Assert.IsAssignableFrom<CatalogMetadata>(metadata);
    }
}