using System;
using System.Collections.Generic;
using Authorization.Models.Dto;
using Authorization.Models.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spred.Bus.Contracts;

namespace Authorization.Test;

public class UpdateUserModelTests
{
    [Fact]
    public void Constructor_ShouldAssignProperties()
    {
        var model = new UpdateUserModel("bio text", "Serbia", "Gleb");

        Assert.Equal("bio text", model.Bio);
        Assert.Equal("Serbia", model.Location);
        Assert.Equal("Gleb", model.Name);
    }

    [Fact]
    public void Records_WithSameValues_ShouldBeEqual()
    {
        var m1 = new UpdateUserModel("bio", "Belgrade", "Alex");
        var m2 = new UpdateUserModel("bio", "Belgrade", "Alex");

        Assert.Equal(m1, m2);
        Assert.True(m1 == m2);
        Assert.Equal(m1.GetHashCode(), m2.GetHashCode());
    }

    [Fact]
    public void Records_WithDifferentValues_ShouldNotBeEqual()
    {
        var m1 = new UpdateUserModel("bio", "Belgrade", "Alex");
        var m2 = new UpdateUserModel("bio", "Novi Sad", "Alex");

        Assert.NotEqual(m1, m2);
        Assert.True(m1 != m2);
    }

    [Fact]
    public void Deconstruct_ShouldReturnValues()
    {
        var model = new UpdateUserModel("bio text", "Serbia", "Gleb");

        var (bio, location, name) = model;

        Assert.Equal("bio text", bio);
        Assert.Equal("Serbia", location);
        Assert.Equal("Gleb", name);
    }
    
     [Fact]
    public void CreateAccountRequest_Should_Hold_Values()
    {
        var r = new CreateAccountRequest("spotify", "acc-1");
        Assert.Equal("spotify", r.Platform);
        Assert.Equal("acc-1", r.AccountId);
        var r2 = r with { AccountId = "acc-2" };
        Assert.Equal("spotify", r2.Platform);
        Assert.Equal("acc-2", r2.AccountId);
    }

    [Fact]
    public void EventPayload_Defaults_And_Setters()
    {
        var e = new EventPayload();
        Assert.Equal(string.Empty, e.EventType);
        Assert.Empty(e.Tags);
        Assert.Equal(default, e.Timestamp);
        e.EventType = "test";
        e.Tags = new Dictionary<string, string> { ["k"] = "v" };
        e.Timestamp = new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc);
        Assert.Equal("test", e.EventType);
        Assert.Equal("v", e.Tags["k"]);
        Assert.Equal(2024, e.Timestamp.Year);
    }

    [Fact]
    public void UserAccountDto_Constructors_Work()
    {
        var d1 = new UserAccountDto();
        Assert.Equal(string.Empty, d1.Platform);
        Assert.Equal(string.Empty, d1.AccountId);
        Assert.Equal(string.Empty, d1.Status);
        Assert.Equal(string.Empty, d1.ProfileUrl);

        var ts = DateTimeOffset.UtcNow;
        var d2 = new UserAccountDto("ytm", "acc-9", "Verified", ts, "https://x");
        Assert.Equal("ytm", d2.Platform);
        Assert.Equal("acc-9", d2.AccountId);
        Assert.Equal("Verified", d2.Status);
        Assert.Equal("https://x", d2.ProfileUrl);
        Assert.Equal(ts, d2.ConnectedAt);
    }

    [Fact]
    public void LinkedAccountEvent_Required_Init_And_Defaults()
    {
        var e = new LinkedAccountEvent
        {
            AccountId = "acc-1",
            UserId = Guid.NewGuid(),
            Platform = AccountPlatform.YouTubeMusic,
            Sequence = 1,
            EventType = LinkedAccountEventType.AccountCreated,
            Payload = null
        };
        Assert.NotEqual(Guid.Empty, e.Id);
        Assert.Equal("acc-1", e.AccountId);
        Assert.NotNull(e.UserId);
        Assert.Equal(AccountPlatform.YouTubeMusic, e.Platform);
        Assert.Equal(1, e.Sequence);
        Assert.Equal(LinkedAccountEventType.AccountCreated, e.EventType);
        Assert.Null(e.Payload);
        Assert.Null(e.ETag);
        Assert.Equal(0L, e.Timestamp);
    }

    [Fact]
    public void LinkedAccountState_Rehydrate_Basic_Lifecycle()
    {
        var seed = new LinkedAccountState
        {
            AccountId = "acc-1",
            UserId = Guid.NewGuid(),
            Platform = AccountPlatform.Spotify
        };

        var createdTs = new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var verifiedTs = new DateTimeOffset(2024, 5, 2, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        var proof = JObject.FromObject(new { token = "abc" });

        var events = new[]
        {
            new LinkedAccountEvent { AccountId = "acc-1", UserId = seed.UserId, Platform = AccountPlatform.Spotify, Sequence = 1, EventType = LinkedAccountEventType.AccountCreated, Payload = null, CorrelationId = Guid.NewGuid() },
            new LinkedAccountEvent { AccountId = "acc-1", UserId = seed.UserId, Platform = AccountPlatform.Spotify, Sequence = 2, EventType = LinkedAccountEventType.TokenIssued, Payload = null, CorrelationId = Guid.NewGuid() },
            new LinkedAccountEvent { AccountId = "acc-1", UserId = seed.UserId, Platform = AccountPlatform.Spotify, Sequence = 3, EventType = LinkedAccountEventType.ProofSubmitted, Payload = proof, CorrelationId = Guid.NewGuid() },
            new LinkedAccountEvent { AccountId = "acc-1", UserId = seed.UserId, Platform = AccountPlatform.Spotify, Sequence = 4, EventType = LinkedAccountEventType.AccountVerified, Payload = null, CorrelationId = Guid.NewGuid() }
        };

        typeof(LinkedAccountEvent).GetProperty(nameof(LinkedAccountEvent.Timestamp))!.SetValue(events[0], createdTs);
        typeof(LinkedAccountEvent).GetProperty(nameof(LinkedAccountEvent.Timestamp))!.SetValue(events[3], verifiedTs);

        var state = LinkedAccountState.Rehydrate(seed, events);

        Assert.Equal(AccountStatus.Verified, state.Status);
        Assert.Equal(4, state.Sequence);
        Assert.Equal(LinkedAccountEventType.AccountVerified, state.LastEventType);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(createdTs), state.CreatedAt);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(verifiedTs), state.VerifiedAt);
        Assert.Equal("abc", state.Proof?["token"]?.Value<string>());
    }

    [Fact]
    public void LinkedAccountState_Ignores_Older_Sequence()
    {
        var seed = new LinkedAccountState
        {
            AccountId = "acc-2",
            UserId = Guid.NewGuid(),
            Platform = AccountPlatform.SoundCloud
        };

        var e1 = new LinkedAccountEvent
        {
            AccountId = "acc-2",
            UserId = seed.UserId,
            Platform = AccountPlatform.SoundCloud,
            Sequence = 10,
            EventType = LinkedAccountEventType.AccountVerified,
            Payload = null
        };
        typeof(LinkedAccountEvent).GetProperty(nameof(LinkedAccountEvent.Timestamp))!.SetValue(e1, 1000L);

        var e0 = new LinkedAccountEvent
        {
            AccountId = "acc-2",
            UserId = seed.UserId,
            Platform = AccountPlatform.SoundCloud,
            Sequence = 5,
            EventType = LinkedAccountEventType.ProofInvalid,
            Payload = null
        };
        typeof(LinkedAccountEvent).GetProperty(nameof(LinkedAccountEvent.Timestamp))!.SetValue(e0, 500L);

        var state = LinkedAccountState.Rehydrate(seed, new[] { e1, e0 });

        Assert.Equal(AccountStatus.Verified, state.Status);
        Assert.Equal(10, state.Sequence);
        Assert.Equal(LinkedAccountEventType.AccountVerified, state.LastEventType);
    }

    [Fact]
    public void LinkedAccountState_Sorts_By_Sequence_Then_Timestamp()
    {
        var seed = new LinkedAccountState
        {
            AccountId = "acc-3",
            UserId = Guid.NewGuid(),
            Platform = AccountPlatform.AppleMusic
        };

        var eA = new LinkedAccountEvent { AccountId = "acc-3", UserId = seed.UserId, Platform = AccountPlatform.AppleMusic, 
            Sequence = 2, EventType = LinkedAccountEventType.ProofSubmitted, Payload = JObject.FromObject(new { k = "v1" }) };
        var eB = new LinkedAccountEvent { AccountId = "acc-3", UserId = seed.UserId, Platform = AccountPlatform.AppleMusic, 
            Sequence = 2, EventType = LinkedAccountEventType.ProofAttached, Payload = JObject.FromObject(new { k = "v2" }) };

        typeof(LinkedAccountEvent).GetProperty(nameof(LinkedAccountEvent.Timestamp))!.SetValue(eA, 2000L);
        typeof(LinkedAccountEvent).GetProperty(nameof(LinkedAccountEvent.Timestamp))!.SetValue(eB, 3000L);

        var state = LinkedAccountState.Rehydrate(seed, new[]
        {
            new LinkedAccountEvent { AccountId = "acc-3", UserId = seed.UserId, Platform = AccountPlatform.AppleMusic, Sequence = 1, EventType = LinkedAccountEventType.AccountCreated, Payload = null, },
            eB,
            eA
        });

        Assert.Equal(2, state.Sequence);
        Assert.Equal("v1", state.Proof?["k"]?.Value<string>());
    }

    [Fact]
    public void LinkedAccountState_AccountLinked_Fills_Missing_Timestamps()
    {
        var seed = new LinkedAccountState
        {
            AccountId = "acc-4",
            UserId = Guid.NewGuid(),
            Platform = AccountPlatform.Deezer
        };

        var t = 7777L;

        var events = new[]
        {
            new LinkedAccountEvent { AccountId = "acc-4", UserId = seed.UserId, Platform = AccountPlatform.Deezer, Sequence = 1, EventType = LinkedAccountEventType.AccountLinked, Payload = null }
        };
        typeof(LinkedAccountEvent).GetProperty(nameof(LinkedAccountEvent.Timestamp))!.SetValue(events[0], t);

        var state = LinkedAccountState.Rehydrate(seed, events);
        Assert.Equal(AccountStatus.Verified, state.Status);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(t), state.CreatedAt);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(t), state.VerifiedAt);
    }

    [Fact]
    public void LinkedAccountState_AccountUnlinked_Sets_Deleted()
    {
        var seed = new LinkedAccountState
        {
            AccountId = "acc-5",
            UserId = Guid.NewGuid(),
            Platform = AccountPlatform.Spotify
        };

        var events = new[]
        {
            new LinkedAccountEvent { AccountId = "acc-5", UserId = seed.UserId, Platform = AccountPlatform.Spotify, Sequence = 1, EventType = LinkedAccountEventType.AccountCreated, Payload = null },
            new LinkedAccountEvent { AccountId = "acc-5", UserId = seed.UserId, Platform = AccountPlatform.Spotify, Sequence = 2, EventType = LinkedAccountEventType.AccountUnlinked, Payload = null }
        };

        var state = LinkedAccountState.Rehydrate(seed, events);
        Assert.Equal(AccountStatus.Deleted, state.Status);
        Assert.Equal(2, state.Sequence);
        Assert.Equal(LinkedAccountEventType.AccountUnlinked, state.LastEventType);
    }

    [Fact]
    public void UserAccountRef_Should_Create_And_Serialize_String_Enum()
    {
        var r = new UserAccountRef(AccountPlatform.YouTubeMusic, "acc-7", "https://p");
        Assert.Equal(AccountPlatform.YouTubeMusic, r.Platform);
        Assert.Equal("acc-7", r.AccountId);
        Assert.Equal("https://p", r.ProfileUrl);

        var json = JsonConvert.SerializeObject(r);
        Assert.Contains("\"Platform\":\"YouTubeMusic\"", json);
    }

    [Fact]
    public void LinkedAccountEvent_Json_Should_Serialize_Enums_As_Strings()
    {
        var e = new LinkedAccountEvent
        {
            AccountId = "acc-8",
            UserId = Guid.NewGuid(),
            Platform = AccountPlatform.SoundCloud,
            Sequence = 42,
            EventType = LinkedAccountEventType.TokenIssued,
            Payload = null
        };
        var json = JsonConvert.SerializeObject(e);
        Assert.Contains("\"Platform\":\"SoundCloud\"", json);
        Assert.Contains("\"EventType\":\"TokenIssued\"", json);
    }
}