using AggregatorService.Abstractions;
using Extensions.Models;
using Microsoft.Extensions.Options;
using Refit;

namespace AggregatorService.Components;

/// <inheritdoc />
public class TrackSenderService : ITrackSenderService
{
    private readonly ITrackServiceApi _trackServiceApi;
    /// <summary>
    /// Initializes a new instance of the <see cref="TrackSenderService"/> class.
    /// </summary>
    /// <param name="outerOptions">Service configuration options containing the TrackService endpoint URL.</param>
    public TrackSenderService(IOptions<ServicesOuterOptions> outerOptions)
    {
        _trackServiceApi = RestService.For<ITrackServiceApi>(outerOptions.Value.TrackService);
    }

    /// <inheritdoc />
    public async Task PushTrack(IFormFile file, Guid id)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{file.FileName}");

        await using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        _ = Task.Run(async () =>
        {
            await using var stream = File.OpenRead(tempFilePath);
            var streamPart = new StreamPart(stream, file.FileName, file.ContentType);
            await _trackServiceApi.AddAudioTrack(Guid.Empty.ToString(), id.ToString(), streamPart);
            File.Delete(tempFilePath);
        });
    }

    /// <inheritdoc />
    public Task UnsuccessfulResult(Guid id)
    {
        _ = Task.Run(async () =>
        {
            await _trackServiceApi.UnsuccessfulResult(Guid.Empty.ToString(), id.ToString());
        });
        
        return Task.CompletedTask;
    }
}