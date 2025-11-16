using System.Linq.Expressions;
using AutoMapper;
using Extensions.Extensions;
using InferenceService.Abstractions;
using InferenceService.Models.Dto;
using InferenceService.Models.Entities;
using Microsoft.Azure.Cosmos;
using Repository.Abstractions.Interfaces;

namespace InferenceService.Components;

/// <summary>
/// Repository for managing inference data.
/// </summary>
public class InferenceManager : IInferenceManager
{
    private readonly IPersistenceStore<InferenceResult, Guid> _persistenceStore;
    private readonly IInferenceAccessService _inferenceAccessService;
    private readonly ILogger<InferenceManager> _logger;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="InferenceManager"/> class.
    /// </summary>
    /// <param name="persistenceStore">The persistence store for inference results.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="mapper">The auto mapper.</param>
    /// <param name="inferenceAccessService">Inference access service.</param>
    public InferenceManager(IPersistenceStore<InferenceResult, Guid> persistenceStore,
        ILoggerFactory loggerFactory, IMapper mapper, IInferenceAccessService inferenceAccessService)
    {
        _persistenceStore = persistenceStore;
        _logger = loggerFactory.CreateLogger<InferenceManager>();
        _mapper = mapper;
        _inferenceAccessService = inferenceAccessService;
    }

    ///<inheritdoc />
    public async Task<InferenceResult> SaveInference(List<InferenceMetadata> results, Guid trackId, Guid spredUserId,
        string modelVersion, CancellationToken cancellationToken)
    {
        var entity = new InferenceResult()
        {
            Metadata = results, SpredUserId = spredUserId,
            ModelVersion = modelVersion, TrackId = trackId
        };
        var result = await _persistenceStore.StoreAsync(entity, cancellationToken);
        if (result.IsSuccess)
            return entity;

        _logger.LogSpredError($"Failed to save inference results for track {trackId} and model version {modelVersion}.",
            result.Exceptions.First());
        throw result.Exceptions.First();
    }

    ///<inheritdoc />
    public async Task<(string, Guid, List<InferenceMetadataDto>?)> GetInference(Guid trackId, Guid spredUserId, bool isPremium,
        string modelVersion, CancellationToken cancellationToken)
    {
        Expression<Func<InferenceResult, bool>> predicate = r => r.ModelVersion == modelVersion && r.SpredUserId == spredUserId;
        Expression<Func<InferenceResult, long>> sortSelector = r => r.Timestamp;

        var result = await _persistenceStore.GetAsync(predicate, sortSelector, 
            new PartitionKey(trackId.ToString()),0, 1, false, cancellationToken, true);

        if (!result.IsSuccess)
            _logger.LogSpredWarning("GetInference",
                $"Failed to get inference results for track {trackId} and model version {modelVersion}. " +
                $"{result.Exceptions.First().Message}");

        if (result.Result?.FirstOrDefault() == null)
            return (modelVersion, Guid.Empty, []);

        var entity = result.Result.First();
        var metadataList = entity.Metadata.OrderByDescending(e => e.Score).ToList();
        var updated = _inferenceAccessService.ApplyVisibilityRulesAsync(
            spredUserId,
            isPremium,
            entity);
        
        if (updated.updated)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            await _persistenceStore.UpdateAsync(entity, cancellationToken);
        }
        
        return (entity.ModelVersion, entity.Id, updated.dtos);

        
    }

    ///<inheritdoc />
    public async Task UpdateInference(Dictionary<string, (string, float)> results, Guid trackId, Guid spredUserId,
        string modelVersion, CancellationToken cancellationToken)
    {
        Expression<Func<InferenceResult, bool>> predicate = r =>
            r.ModelVersion == modelVersion && r.SpredUserId == spredUserId;
        Expression<Func<InferenceResult, long>> sortSelector = r => r.Timestamp;

        var result = await _persistenceStore.GetAsync(predicate, sortSelector, new PartitionKey(trackId.ToString()), 0, 1, false,
            cancellationToken, true);

        if (result.IsSuccess && result.Result?.FirstOrDefault() != null)
        {
            var entity = result.Result.First();
            entity.UpdatedAt = DateTime.Now;
            entity.Metadata = results.Select(r => new InferenceMetadata()
            {
                MetadataId = Guid.Parse(r.Key),
                Score = r.Value.Item2,
                MetadataOwner = Guid.Parse(r.Value.Item1),
                Reaction = new ReactionStatus()
            }).ToList();
            await _persistenceStore.UpdateAsync(entity, cancellationToken);
        }
    }

    ///<inheritdoc />
    public async Task AddRateToPlaylist(Guid playlistId, Guid trackId, Guid spredUserId, string modelVersion,
        ReactionStatus reaction, CancellationToken cancellationToken)
    {
        Expression<Func<InferenceResult, bool>> predicate = r => r.ModelVersion == modelVersion && r.SpredUserId == spredUserId;
        Expression<Func<InferenceResult, long>> sortSelector = r => r.Timestamp;

        var result = await _persistenceStore.GetAsync(predicate, sortSelector, new PartitionKey(trackId.ToString()), 
            0, 1, false, cancellationToken, true);

        if (result.IsSuccess && result.Result?.FirstOrDefault() != null)
        {
            var entity = result.Result.First();
            var playlist = entity.Metadata.FirstOrDefault(p => p.MetadataId == playlistId);

            if (playlist == null)
                return;
            
            if(reaction.IsLiked != null)
                playlist.Reaction.IsLiked = reaction.IsLiked;
            if(reaction.HasApplied != null)
                playlist.Reaction.HasApplied = reaction.HasApplied;
            if(reaction.WasAccepted != null)
                playlist.Reaction.WasAccepted = reaction.WasAccepted;
            
            entity.UpdatedAt = DateTime.Now;
            
            await _persistenceStore.UpdateAsync(entity, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task UpdateInference(Guid id, Guid trackId, Dictionary<TrackMetadataPair, List<SimilarTrack>> similarTrack, CancellationToken cancellationToken)
    {
        var inference = await _persistenceStore.GetAsync(id, new PartitionKey(trackId.ToString()), 
                cancellationToken, true);
        if (inference is { IsSuccess: true, Result: not null })
        {
            foreach (var s in similarTrack)
            {
                var metadata = inference.Result.Metadata.FirstOrDefault(i => i.MetadataId == s.Key.MetadataId);
                if(metadata != null)
                    metadata.SimilarTracks = s.Value;
            }
            
            await _persistenceStore.UpdateAsync(inference.Result, cancellationToken);
        }
    }
}