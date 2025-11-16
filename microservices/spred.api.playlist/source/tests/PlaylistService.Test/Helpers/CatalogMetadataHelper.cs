using PlaylistService.Test.TestData;
using PlaylistService.Models.Entities;

namespace PlaylistService.Test.Helpers;

public static class CatalogMetadataHelper
{
    public static Guid Id = Guid.NewGuid();
    
    public static CatalogMetadata InitTestObject2(bool isPublic = true)
    {
        var obj = new TestCatalogMetadata()
        { };
        obj.SetId(Id, isPublic);
        return obj;
    }
    
    public static CatalogMetadata InitTestObject()
    {
        var obj = new TestCatalogMetadata()
            { };
        obj.SetId(Id);
        return obj;
    }
}