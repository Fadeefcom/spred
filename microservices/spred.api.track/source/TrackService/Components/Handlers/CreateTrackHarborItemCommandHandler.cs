using AutoMapper;
using Exception.Exceptions;
using Extensions.Extensions;
using MassTransit;
using MediatR;
using Spred.Bus.Contracts;
using StackExchange.Redis;
using TrackService.Abstractions;
using TrackService.Components.Services;
using TrackService.Helpers;
using TrackService.Models.Commands;
using TrackService.Models.Entities;

namespace TrackService.Components.Handlers;

/// <summary>
/// Handles the creation of track metadata items.
/// </summary>
public class CreateTrackServiceItemCommandHandler : IRequestHandler<CreateTrackMetadataItemCommand, Guid>
{
    private readonly List<string> _validCodec = ["mp3", "flac", "pcm"];
    private readonly IUploadTrackService _uploadTrackService;
    private readonly ITrackManager _trackManager;
    private readonly IAnalayzeTrackService _analayzeTrackService;
    private readonly ILogger<CreateTrackServiceItemCommandHandler> _logger;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly IDatabase _database;
    private readonly TrackPlatformLinkService _trackPlatformLinkService;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    /// <summary>
    /// Handles the creation of track metadata items.
    /// </summary>
    /// <param name="uploadTrackService">The service for uploading track files.</param>
    /// <param name="trackManager">The repository for track metadata items.</param>
    /// <param name="analayzeTrackService">The service for analyzing track files.</param>
    /// <param name="sendEndpointProvider">The queue service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="connectionMultiplexer">Redis connection.</param>
    /// <param name="trackPlatformLinkService">Track link service.</param>
    /// <param name="mediator">Mediator.</param>
    public CreateTrackServiceItemCommandHandler(IUploadTrackService uploadTrackService,
        ITrackManager trackManager,
        IAnalayzeTrackService analayzeTrackService,
        ILogger<CreateTrackServiceItemCommandHandler> logger,
        ISendEndpointProvider sendEndpointProvider,
        IConnectionMultiplexer connectionMultiplexer,
        TrackPlatformLinkService trackPlatformLinkService,
        IMediator mediator,
        IMapper mapper)
    {
        _uploadTrackService = uploadTrackService;
        _trackManager = trackManager;
        _analayzeTrackService = analayzeTrackService;
        _logger = logger;
        _sendEndpointProvider = sendEndpointProvider;
        _database = connectionMultiplexer.GetDatabase();
        _trackPlatformLinkService = trackPlatformLinkService;
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the creation of a track metadata item.
    /// </summary>
    /// <param name="request">The command to create a track metadata item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unique identifier of the created track metadata item.</returns>
    /// <exception cref="BaseException">Thrown when the creation fails.</exception>
    public async Task<Guid> Handle(CreateTrackMetadataItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.PlatformIds != null && request.PlatformIds.Count != 0)
            {
                var existingTrackId = await _trackPlatformLinkService.GetLinkAsync(request.PlatformIds, cancellationToken);
                if (existingTrackId.HasValue && existingTrackId != Guid.Empty)
                {
                    var updateCommand = _mapper.Map<UpdateTrackMetadataItemCommand>(request);
                    await _mediator.Publish(updateCommand, cancellationToken);

                    return existingTrackId.Value;
                }
            }

            if (string.IsNullOrWhiteSpace(request.ImageUrl))
                request.UpdateImageUrl(await ImageService.GetFinalImageUrlAsync());

            if (request.FormFile != null) 
            {
                request.Path = await ByteFileReader.SaveFile(request.FormFile);
                var analayze = await _analayzeTrackService.Analayze(request.Path, cancellationToken);
                request.UpdateByTrackAnalayze(analayze);
                        
                if(!_validCodec.Contains(request.Audio.Codec.Split('_')[0]))
                    throw new BaseException("Invalid codec", 400, "Invalid codec");
            }
        
            int attempts = 3;
            var item = new TrackMetadata();
            item.Create(request);

            bool cosmosAdded = false;
            bool blobAdded = false;
            bool sendToInference = false;
            
            while (attempts > 0)
            {
                try
                {
                    if (!blobAdded && request.FormFile != null)
                    {
                        await using var stream = File.OpenRead(request.Path);
                        await _uploadTrackService.UploadTrackAsync(stream, item.Id, cancellationToken);
                        blobAdded = true;

                        item.StatusCreated();
                    }
                    
                    if (!cosmosAdded)
                    {
                        var trackId = await _trackManager.AddAsync(item, request.SpredUserId, cancellationToken);
                        if (request.PlatformIds != null && request.PlatformIds.Count != 0)
                            await _trackPlatformLinkService.AddLinksAsync(request.PlatformIds, request.SpredUserId, trackId, cancellationToken);
                        cosmosAdded = true;
                    }

                    if (blobAdded && !sendToInference)
                    {
                        // If SpredUserId is not empty, it means the track was submitted by a user (not a parser),
                        // so we proceed to publish it for further analysis.
                        if (request.SpredUserId != Guid.Empty)
                        {
                            var message = new InferenceRequest
                            {
                                SpredUserId = request.SpredUserId,
                                TrackId = item.Id
                            };

                            var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("exchange:inference-request"));
                            await endpoint.Send(message, CancellationToken.None);
                            
                            var cacheKey = $"inference:{message.TrackId}:{message.SpredUserId}";
                            await _database.StringSetAsync(cacheKey, "pending", TimeSpan.FromMinutes(15), When.NotExists, CommandFlags.FireAndForget);
                        }
                        
                        sendToInference = true;
                    }

                    return item.Id;
                }
                catch(System.Exception ex)
                {
                    attempts--;
                    _logger.LogSpredError("TrackUpload", $"Failed to add track {item.Id} to repository, " +
                                         $"cosmosAdded:{cosmosAdded}, blobAdded:{blobAdded}, sendToInference:{sendToInference}", ex);
                    if(!cosmosAdded && !blobAdded)
                        item.ResetId();

                    if (attempts == 0)
                        throw;
                }
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogSpredError("TrackUpload",$"Unexpected error while handling track creation", ex);
            throw;
        }
        finally
        {
            if (File.Exists(request.Path))
                File.Delete(request.Path);
        }

        return Guid.Empty;
    }
}
