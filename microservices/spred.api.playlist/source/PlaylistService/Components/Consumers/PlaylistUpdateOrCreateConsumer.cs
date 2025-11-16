using AutoMapper;
using Extensions.Extensions;
using Extensions.Models;
using MassTransit;
using MediatR;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using PlaylistService.Abstractions;
using PlaylistService.Models.Commands;
using PlaylistService.Models.Entities;
using Refit;
using Repository.Abstractions.Interfaces;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;

namespace PlaylistService.Components.Consumers;

/// <summary>
/// Consumer that handles <see cref="CatalogEnrichmentUpdateOrCreate"/> messages coming from external services.
/// Responsible for processing playlist updates by enriching track data, deduplicating statistics,
/// and publishing an internal <see cref="UpdateMetadataCommand"/> using MediatR.
/// </summary>
public sealed class PlaylistUpdateOrCreateConsumer : IConsumer<CatalogEnrichmentUpdateOrCreate>
{
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly IPersistenceStore<CatalogStatistics, Guid> _statisticsStore;
    private readonly ITrackServiceApi _trackService;
    private readonly ILogger<PlaylistUpdateOrCreateConsumer> _logger;
    
    private const int MaxTracksPerPlaylist = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistUpdateOrCreateConsumer"/> class.
    /// </summary>
    /// <param name="mapper">AutoMapper instance used to convert external messages into internal commands.</param>
    /// <param name="mediator">MediatR instance used to publish internal domain commands.</param>
    /// <param name="loggerFactory">Logger factory for creating a logger specific to this consumer.</param>
    /// <param name="statisticsStore">Store interface for persisting catalog statistics.</param>
    /// <param name="options">Service configuration options including external TrackService URL.</param>
    public PlaylistUpdateOrCreateConsumer(IMapper mapper, IMediator mediator, ILoggerFactory loggerFactory, 
        IPersistenceStore<CatalogStatistics, Guid> statisticsStore, IOptions<ServicesOuterOptions> options)
    {
        _mapper = mapper;
        _mediator = mediator;
        _statisticsStore = statisticsStore;
        _logger = loggerFactory.CreateLogger<PlaylistUpdateOrCreateConsumer>();
        _trackService = RestService.For<ITrackServiceApi>(options.Value.TrackService);
    }

    /// <summary>
    /// Handles the incoming <see cref="CatalogEnrichmentUpdateOrCreate"/> message by:
    /// 1. Mapping it to an internal update command.
    /// 2. Enriching it with added track IDs.
    /// 3. Updating statistics in the database.
    /// 4. Publishing the command for further processing.
    /// </summary>
    /// <param name="context">Message context containing the <see cref="CatalogEnrichmentUpdateOrCreate"/>.</param>
    /// <returns>A completed <see cref="Task"/> once processing is finished.</returns>
    public async Task Consume(ConsumeContext<CatalogEnrichmentUpdateOrCreate> context)
    {
        _logger.LogSpredInformation("Consume", $"Received UpdateOrCreate message with Id: {context.Message.Snapshot.Id}, {context.CorrelationId}");

        var message = context.Message;

        object entity;

        if (message.Snapshot.Id == null || message.Snapshot.Id == Guid.Empty)
        {
            var createCommand = _mapper.Map<CreateMetadataCommand>(message.Snapshot);
            
            List<Guid> trackIds = [];
            foreach (var track in message.Tracks.OrderBy(t => t.AddedAt).ToList())
            {
                var result = await AddTrack(track, createCommand.SpredUserId);
                if (result != Guid.Empty)
                    trackIds.Add(result);

                if (trackIds.Count >= MaxTracksPerPlaylist)
                    break;
            }

            createCommand.Tracks = trackIds;
            await AddStatistics(message.Stats, createCommand.Id);
            createCommand.Status = FetchStatus.FetchedTracks;

            entity = createCommand;
        }
        else
        {
            var updateCommand = _mapper.Map<UpdateMetadataCommand>(message.Snapshot);

            List<Guid> trackIds = [];
            foreach (var track in message.Tracks.OrderBy(t => t.AddedAt).ToList())
            {
                var result = await AddTrack(track, updateCommand.SpredUserId);
                if (result != Guid.Empty)
                    trackIds.Add(result);

                if (trackIds.Count >= MaxTracksPerPlaylist)
                    break;
            }

            updateCommand.Tracks = trackIds;
            await AddStatistics(message.Stats, updateCommand.Id);
            updateCommand.StatsUpdated = true;
            updateCommand.Status = null;

            entity = updateCommand;
        }

        await _mediator.Publish(entity);

        _logger.LogSpredInformation("Consume", $"Finished processing {((dynamic)entity).Id}");
    }

    private async Task AddStatistics(HashSet<StatInfo> command, Guid id)
    {
        var catalogStatistics = command.Select(s => new CatalogStatistics(s, id)).ToList();

        var statsFromDbResult = await _statisticsStore.GetAsync(
            predicate: s => true,
            sortSelector: s => s.Timestamp,
            partitionKey: new PartitionKey(id.ToString()),
            offset: 0,
            limit: 10,
            descending: true,
            cancellationToken: CancellationToken.None
        );

        var statsFromDb = statsFromDbResult.Result?.ToList() ?? [];

        var existingDates = statsFromDb
            .Select(s => s.Date)
            .ToHashSet();

        var newStats = catalogStatistics
            .Where(s => !existingDates.Contains(s.Date))
            .ToList();

        List<Task> tasks = [];
        if (statsFromDb.Count > 0 && newStats.Count > 0)
        {
            var latestOld = statsFromDb[0]; // (descending=true)
            var nextNew = newStats.FirstOrDefault(s => s.Date > latestOld.Date);

            if (nextNew != null)
            {
                latestOld.FollowersDailyDiff = (int)(nextNew.Followers - latestOld.Followers);
                tasks.Add(_statisticsStore.UpdateAsync(statsFromDb[0], CancellationToken.None));
            }
        }

        tasks.AddRange(newStats.Select(stat =>
            _statisticsStore.StoreAsync(stat, CancellationToken.None)));
        await Task.WhenAll(tasks);
    }

    private async Task<Guid> AddTrack(TrackDtoWithPlatformIds track, Guid spredUserId)
    {
        var response = await _trackService.AddTrack(spredUserId.ToString(), track);

        if (response.IsSuccessful)
            return response.Content.GetProperty("id").GetGuid();

        _logger.LogSpredWarning("PlaylistUpdateConsumerAddTrack",
            $"Failed to add track for user - {spredUserId} and track - {track.PrimaryId}. StatusCode: {response.StatusCode}, Error: {response.ReasonPhrase}");
        return Guid.Empty;
    }
}