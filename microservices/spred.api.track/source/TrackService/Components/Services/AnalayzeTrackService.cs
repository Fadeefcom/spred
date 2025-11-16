using Exception;
using TrackService.Abstractions;
using TrackService.Models.DTOs;
using Xabe.FFmpeg;

namespace TrackService.Components.Services;
/// <summary>
/// Provides functionality to analyze audio tracks.
/// </summary>
public class AnalayzeTrackService : IAnalayzeTrackService
{
    private readonly IFFmpegWrapper _ffmpegWrapper;
    
    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="ffmpegWrapper"></param>
    public AnalayzeTrackService(IFFmpegWrapper ffmpegWrapper)
    {
        _ffmpegWrapper = ffmpegWrapper;
    }
    
    /// <summary>
    /// Analyzes the specified audio track file and returns its metadata.
    /// </summary>
    /// <param name="filePath">The path to the audio track file.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the metadata of the analyzed track.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the file is not readable.</exception>
    public async Task<AnalyzeTrackDto> Analayze(string filePath, CancellationToken cancellationToken)
    {
        var originalInfo = await _ffmpegWrapper.GetMediaInfo(filePath, cancellationToken);
        var originalAudio = originalInfo.AudioStreams.FirstOrDefault();
        originalAudio.ThrowBaseExceptionIfNull("File is not readable.");

        var newFilePath = await Compression(originalAudio!, filePath);

        var finalInfo = await _ffmpegWrapper.GetMediaInfo(newFilePath, cancellationToken);
        var finalAudio = finalInfo.AudioStreams.FirstOrDefault();

        finalAudio.ThrowBaseExceptionIfNull("Failed to read compressed audio stream.");

        return new AnalyzeTrackDto
        {
            Bitrate = (uint?)finalAudio?.Bitrate ?? 0,
            Channels = (ushort?)finalAudio?.Channels ?? 0,
            Duration = finalAudio?.Duration ?? TimeSpan.Zero,
            SampleRate = (uint?)finalAudio?.SampleRate ?? 0,
            Codec = finalAudio?.Codec ?? string.Empty,
            Bpm = 0,
            FilePath = newFilePath
        };
    }
    
    private async Task<string> Compression(IAudioStream audioStream, string filePath)
    {
        if (audioStream.Codec.Contains("pcm", StringComparison.OrdinalIgnoreCase))
        {
            var outputFile = Path.Combine(
                Environment.CurrentDirectory,
                Models.Names.AudioFiles,
                Path.ChangeExtension(Path.GetRandomFileName(), ".mp3")
            );

            var conversion = _ffmpegWrapper.CreateConversion()
                .AddParameter($"-i \"{filePath}\"", ParameterPosition.PreInput)
                .AddParameter($"-ar {audioStream.SampleRate}")
                .AddParameter($"-ac {audioStream.Channels}")
                .AddParameter($"-b:a {audioStream.Bitrate}k")
                .SetOutput(outputFile)
                .SetOutputFormat(Format.mp3)
                .SetOverwriteOutput(true);

            await conversion.Start();
            return outputFile;
        }

        return filePath;
    }
}
