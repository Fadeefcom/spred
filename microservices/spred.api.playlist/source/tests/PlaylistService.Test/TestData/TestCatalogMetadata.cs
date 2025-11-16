using PlaylistService.Models;
using PlaylistService.Models.Entities;

namespace PlaylistService.Test.TestData;

public class TestCatalogMetadata : CatalogMetadata
{
    public void SetId(Guid id, bool isPublic = true)
    {
        Id = id;
        IsPublic = isPublic;
        Type = "playlist";
        IsDeleted = false;
        PrimaryId = new PrimaryId("test", Type, "empty");
    }
}