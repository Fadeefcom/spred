using TrackService.Models.Entities;
using TrackService.Test.TestData;

namespace TrackService.Test.Helpers;

public static class TestObjectsHelper
{
    public static TrackMetadata InitTestTrackMetadata()
    {
        var obj = new TestTrackMetadata()
        {
            Artists = {  },
        };
        obj.SetId(Guid.Empty);
        return obj;
    }
}