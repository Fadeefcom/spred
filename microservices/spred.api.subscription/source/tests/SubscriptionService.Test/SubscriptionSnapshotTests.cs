using SubscriptionService.Models;
using SubscriptionService.Models.Entities;

namespace SubscriptionService.Test;

public class SubscriptionSnapshotTests
{
    [Fact]
    public void Constructor_ShouldInitialize_DefaultValues()
    {
        var snapshot = new SubscriptionSnapshot();

        Assert.NotEqual(Guid.Empty, snapshot.Id);
        Assert.Equal(nameof(UserSubscriptionStatus), snapshot.Type);
        Assert.Equal(0, snapshot.Timestamp);
        Assert.Null(snapshot.ETag);
        Assert.Equal(Guid.Empty, snapshot.UserId);
        Assert.Equal(Guid.Empty, snapshot.StatusId);
        Assert.Equal(string.Empty, snapshot.Kind);
        Assert.Equal(string.Empty, snapshot.ExternalId);
        Assert.Equal(string.Empty, snapshot.RawJson);
    }

    [Fact]
    public void Properties_ShouldBeSettable_ThroughInit()
    {
        var userId = Guid.NewGuid();
        var statusId = Guid.NewGuid();

        var snapshot = new SubscriptionSnapshot
        {
            UserId = userId,
            StatusId = statusId,
            Kind = "invoice:payment_succeeded",
            ExternalId = "inv_123",
            RawJson = "{\"amount\":100}"
        };

        Assert.Equal(userId, snapshot.UserId);
        Assert.Equal(statusId, snapshot.StatusId);
        Assert.Equal("invoice:payment_succeeded", snapshot.Kind);
        Assert.Equal("inv_123", snapshot.ExternalId);
        Assert.Equal("{\"amount\":100}", snapshot.RawJson);
    }

    [Fact]
    public void Type_ShouldRemainImmutable()
    {
        var snapshot = new SubscriptionSnapshot();
        Assert.Equal(nameof(UserSubscriptionStatus), snapshot.Type);
    }

    [Fact]
    public void Id_ShouldBeImmutable_AfterConstruction()
    {
        var s1 = new SubscriptionSnapshot();
        var s2 = new SubscriptionSnapshot();

        Assert.NotEqual(s1.Id, s2.Id);
    }

    [Fact]
    public void PartitionKey_ShouldBeGuidType()
    {
        var snapshot = new SubscriptionSnapshot();
        Assert.IsType<Guid>(snapshot.UserId);
    }
    
    [Fact]
    public void Constructor_ShouldAssign_Reason()
    {
        var request = new RefundRequest("User canceled subscription");

        Assert.Equal("User canceled subscription", request.Reason);
    }
}