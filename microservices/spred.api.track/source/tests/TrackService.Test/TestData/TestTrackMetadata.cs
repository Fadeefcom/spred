using TrackService.Models.Entities;

namespace TrackService.Test.TestData;

public class TestTrackMetadata : TrackMetadata
{
    public void SetId(Guid id, bool isPublic = true)
    {
        Id = id;
        IsDeleted = false;
        SpredUserId = Guid.Empty;
    }
}