using System;
using Authorization.Models.Entities;
using Newtonsoft.Json.Linq;
using Spred.Bus.Contracts;

namespace Authorization.Test;

public class LinkedAccountStateTests
{
    private static LinkedAccountEvent CreateEvent(
        LinkedAccountEventType type,
        long seq = 1,
        long timestamp = 1000,
        JObject? payload = null)
    {
        return new LinkedAccountEvent
        {
            AccountId = "acc",
            UserId = Guid.NewGuid(),
            Platform = AccountPlatform.Spotify,
            EventType = type,
            Sequence = seq,
            CorrelationId = Guid.NewGuid(),
            Payload = payload
        };
    }

    [Fact]
    public void Apply_ShouldHandle_AccountCreated()
    {
        var state = new LinkedAccountState { AccountId = "acc", UserId = Guid.NewGuid(), Platform = AccountPlatform.Spotify };
        var e = CreateEvent(LinkedAccountEventType.AccountCreated);

        var rehydrated = LinkedAccountState.Rehydrate(state, new[] { e });

        Assert.Equal(AccountStatus.Pending, rehydrated.Status);
        Assert.Equal(e.CorrelationId, rehydrated.CorrelationId);
        Assert.Equal(e.EventType, rehydrated.LastEventType);
        Assert.NotEqual(default, rehydrated.CreatedAt);
    }

    [Fact]
    public void Apply_ShouldHandle_TokenIssued()
    {
        var state = new LinkedAccountState { AccountId = "acc", UserId = Guid.NewGuid(), Platform = AccountPlatform.Spotify };
        var e = CreateEvent(LinkedAccountEventType.TokenIssued);

        var rehydrated = LinkedAccountState.Rehydrate(state, new[] { e });

        Assert.Equal(AccountStatus.TokenIssued, rehydrated.Status);
        Assert.Equal(LinkedAccountEventType.TokenIssued, rehydrated.LastEventType);
    }

    [Fact]
    public void Apply_ShouldHandle_ProofSubmitted()
    {
        var state = new LinkedAccountState { AccountId = "acc", UserId = Guid.NewGuid(), Platform = AccountPlatform.Spotify };
        var payload = JObject.FromObject(new { token = "abc" });
        var e = CreateEvent(LinkedAccountEventType.ProofSubmitted, payload: payload);

        var rehydrated = LinkedAccountState.Rehydrate(state, new[] { e });

        Assert.Equal(AccountStatus.ProofSubmitted, rehydrated.Status);
        Assert.Equal(payload, rehydrated.Proof);
    }

    [Fact]
    public void Apply_ShouldHandle_ProofAttached()
    {
        var state = new LinkedAccountState { AccountId = "acc", UserId = Guid.NewGuid(), Platform = AccountPlatform.Spotify };
        var payload = JObject.FromObject(new { extra = true });
        var e = CreateEvent(LinkedAccountEventType.ProofAttached, payload: payload);

        var rehydrated = LinkedAccountState.Rehydrate(state, new[] { e });

        Assert.Equal(payload, rehydrated.Proof);
        Assert.Equal(LinkedAccountEventType.ProofAttached, rehydrated.LastEventType);
    }

    [Fact]
    public void Apply_ShouldHandle_ProofInvalid()
    {
        var state = new LinkedAccountState { AccountId = "acc", UserId = Guid.NewGuid(), Platform = AccountPlatform.Spotify };
        var e = CreateEvent(LinkedAccountEventType.ProofInvalid);

        var rehydrated = LinkedAccountState.Rehydrate(state, new[] { e });

        Assert.Equal(AccountStatus.Error, rehydrated.Status);
    }

    [Fact]
    public void Apply_ShouldHandle_AccountVerified()
    {
        var state = new LinkedAccountState { AccountId = "acc", UserId = Guid.NewGuid(), Platform = AccountPlatform.Spotify };
        var e = CreateEvent(LinkedAccountEventType.AccountVerified);

        var rehydrated = LinkedAccountState.Rehydrate(state, new[] { e });

        Assert.Equal(AccountStatus.Verified, rehydrated.Status);
        Assert.NotNull(rehydrated.VerifiedAt);
    }

    [Fact]
    public void Apply_ShouldHandle_AccountLinked()
    {
        var state = new LinkedAccountState { AccountId = "acc", UserId = Guid.NewGuid(), Platform = AccountPlatform.Spotify };
        var e = CreateEvent(LinkedAccountEventType.AccountLinked);

        var rehydrated = LinkedAccountState.Rehydrate(state, new[] { e });

        Assert.Equal(AccountStatus.Verified, rehydrated.Status);
        Assert.NotNull(rehydrated.VerifiedAt);
        Assert.NotEqual(default, rehydrated.CreatedAt);
    }

    [Fact]
    public void Apply_ShouldHandle_AccountUnlinked()
    {
        var state = new LinkedAccountState { AccountId = "acc", UserId = Guid.NewGuid(), Platform = AccountPlatform.Spotify };
        var e = CreateEvent(LinkedAccountEventType.AccountUnlinked);

        var rehydrated = LinkedAccountState.Rehydrate(state, new[] { e });

        Assert.Equal(AccountStatus.Deleted, rehydrated.Status);
    }

    [Fact]
    public void Apply_ShouldIgnore_EventWithLowerOrEqualSequence()
    {
        var state = new LinkedAccountState { AccountId = "acc", UserId = Guid.NewGuid(), Platform = AccountPlatform.Spotify };
        var e1 = CreateEvent(LinkedAccountEventType.TokenIssued, seq: 5);
        var e2 = CreateEvent(LinkedAccountEventType.ProofInvalid, seq: 5);

        var rehydrated = LinkedAccountState.Rehydrate(state, new[] { e1, e2 });

        Assert.Equal(AccountStatus.TokenIssued, rehydrated.Status);
        Assert.Equal(LinkedAccountEventType.TokenIssued, rehydrated.LastEventType);
    }
}