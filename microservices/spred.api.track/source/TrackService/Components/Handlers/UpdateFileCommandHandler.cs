using AutoMapper;
using Extensions.Utilities;
using MediatR;
using TrackService.Abstractions;
using TrackService.Helpers;
using TrackService.Models.Commands;

namespace TrackService.Components.Handlers;

/// <summary>
/// Handles the update of a file associated with a track.
/// </summary>
public class UpdateFileCommandHandler : INotificationHandler<UpdateFileCommand>
{
    private readonly IAnalayzeTrackService _analyticsService;
    private readonly IUploadTrackService _uploadService;
    private readonly ITrackManager _trackManager;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateFileCommandHandler"/> class.
    /// </summary>
    /// <param name="uploadService">The service for uploading track files.</param>
    /// <param name="analyticsService">The service for analyzing track metadata.</param>
    /// <param name="trackManager">The manager for track metadata operations.</param>
    /// <param name="mapper">The mapper.</param>
    public UpdateFileCommandHandler(
        IUploadTrackService uploadService,
        IAnalayzeTrackService analyticsService,
        ITrackManager trackManager,
        IMapper mapper)
    {
        _uploadService = uploadService;
        _analyticsService = analyticsService;
        _trackManager = trackManager;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the update file command by saving the file, analyzing its metadata, 
    /// uploading the file, and updating the associated track metadata.
    /// </summary>
    /// <param name="notification">The update file command containing the file and track details.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task Handle(UpdateFileCommand notification, CancellationToken cancellationToken)
    {
        var bucket = notification.SpredUserId == Guid.Empty
            ? GuidShortener.GenerateBucketFromGuid(notification.Id)
            : "00";
        var item = await _trackManager.GetByIdAsync(notification.Id, notification.SpredUserId, cancellationToken, bucket);

        if (item != null)
        {
            // Save the file locally and analyze its metadata
            var path = await ByteFileReader.SaveFile(notification.File);
            var analayze = await _analyticsService.Analayze(path, cancellationToken);

            // Upload the file to the storage service
            await using var stream = File.OpenRead(path);
            await _uploadService.UploadTrackAsync(stream, notification.Id, cancellationToken);

            var updateCommand = new UpdateTrackMetadataItemCommand();
            updateCommand.UpdateByTrackAnalayze(analayze);
            
            item.StatusCreated();
            item.Update(updateCommand);
            await _trackManager.UpdateAsync(item, cancellationToken);
        }
    }
}
