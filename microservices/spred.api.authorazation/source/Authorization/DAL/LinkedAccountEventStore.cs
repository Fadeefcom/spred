using Authorization.Abstractions;
using Authorization.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using Repository.Abstractions.Interfaces;
using Spred.Bus.Contracts;

namespace Authorization.DAL;

/// <inheritdoc/>
public sealed class LinkedAccountEventStore : ILinkedAccountEventStore
{
    private readonly IPersistenceStore<LinkedAccountEvent, Guid> _events;
    private readonly ILogger<LinkedAccountEventStore> _logger;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="eventsStore"></param>
    /// <param name="logger"></param>
    public LinkedAccountEventStore(
        IPersistenceStore<LinkedAccountEvent, Guid> eventsStore,
        ILogger<LinkedAccountEventStore> logger)
    {
        _events = eventsStore;
        _logger = logger;
    }
    
    /// <inheritdoc/>
    public async Task<LinkedAccountState?> GetCurrentState(string accountId, AccountPlatform platform, Guid userId, CancellationToken cancellationToken)
    {
        var pk = new PartitionKeyBuilder().Add(platform.ToString()).Add(accountId).Build();
        
        var events = new List<LinkedAccountEvent>();

        await foreach (var e in _events.GetAllAsync(pk, cancellationToken))
        {
            if(e.UserId != userId)
                continue;
            
            events.Add(e);
        }

        if (events.Count == 0)
            return null;
        
        var state = new LinkedAccountState
        {
            AccountId = accountId,
            UserId = userId,
            Platform = platform
        };
        LinkedAccountState.Rehydrate(state, events);
        
        return state;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> AppendAsync(
        string accountId, Guid userId, AccountPlatform platform, LinkedAccountEventType type, JObject? payload,
        CancellationToken cancellationToken)
    {
        var state = await GetCurrentState(accountId, platform, userId, cancellationToken);

        if (!IsTransitionAllowed(state?.LastEventType, type))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidStateTransition",
                Description = $"Cannot transition from {state?.LastEventType} to {type}"
            });
        }

        var sequence = (state?.Sequence ?? 0) + 1;

        var entity = new LinkedAccountEvent
        {
            AccountId = accountId,
            EventType = type,
            Payload = payload,
            Platform = platform,
            Sequence = sequence,
            UserId = userId,
            CorrelationId = state?.CorrelationId ?? Guid.NewGuid()
        };

        var result = await _events.StoreAsync(entity, cancellationToken);
        return result.IsSuccess
            ? IdentityResult.Success
            : IdentityResult.Failed(result.Exceptions.Select(e => new IdentityError { Code = e.Message }).ToArray());
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> UnlinkAsync(string accountId, Guid userId, AccountPlatform platform, CancellationToken cancellationToken)
    {
        var state = await GetCurrentState(accountId, platform, userId, cancellationToken);
        
        if (state?.LastEventType != null)
        {
            var entity = new LinkedAccountEvent()
            {
                AccountId = accountId,
                EventType = LinkedAccountEventType.AccountUnlinked,
                Payload = null,
                Platform = platform,
                Sequence = state.Sequence + 1,
                UserId = userId,
                CorrelationId = state.CorrelationId
            };
            var result = await _events.StoreAsync(entity, cancellationToken);
            
            return result.IsSuccess ? IdentityResult.Success : IdentityResult.Failed(result.Exceptions.Select(e => new IdentityError { Code = e.Message }).ToArray());
        }
        
        return IdentityResult.Failed(new IdentityError { Code = "AccountUnlinkedFailed" });
    }
    
    private static bool IsTransitionAllowed(LinkedAccountEventType? lastEvent, LinkedAccountEventType newEvent)
    {
        if (lastEvent == null)
        {
            return newEvent is LinkedAccountEventType.AccountCreated or LinkedAccountEventType.AccountLinked;
        }

        return _allowedTransitions.TryGetValue(lastEvent.Value, out var allowed) 
               && allowed.Contains(newEvent);
    }
    
    private static readonly Dictionary<LinkedAccountEventType, LinkedAccountEventType[]> _allowedTransitions =
        new()
        {
            { LinkedAccountEventType.AccountCreated, [LinkedAccountEventType.TokenIssued] },
            { LinkedAccountEventType.TokenIssued, [LinkedAccountEventType.ProofSubmitted] },
            { LinkedAccountEventType.ProofSubmitted, [LinkedAccountEventType.ProofAttached, LinkedAccountEventType.ProofInvalid] },
            { LinkedAccountEventType.ProofAttached, [LinkedAccountEventType.ProofInvalid, LinkedAccountEventType.AccountVerified] },
            { LinkedAccountEventType.ProofInvalid, [LinkedAccountEventType.TokenIssued, LinkedAccountEventType.ProofAttached] },
            { LinkedAccountEventType.AccountVerified, [LinkedAccountEventType.AccountLinked] },
            { LinkedAccountEventType.AccountLinked, [LinkedAccountEventType.AccountUnlinked] },
            { LinkedAccountEventType.AccountUnlinked, [LinkedAccountEventType.AccountCreated, LinkedAccountEventType.AccountLinked] }
        };
}