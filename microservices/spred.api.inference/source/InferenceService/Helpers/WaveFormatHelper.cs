using System.Buffers;
using InferenceService.Abstractions;
using InferenceService.Models;
using Xabe.FFmpeg;

namespace InferenceService.Helpers;

/// <summary>
/// Helper class for generating waveform data from audio files.
/// </summary>
public class WaveFormatHelper
{
    private readonly IFFmpegWrapper _ffmpegWrapper;
    
    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="ffmpegWrapper"></param>
    public WaveFormatHelper(IFFmpegWrapper ffmpegWrapper)
    {
        _ffmpegWrapper = ffmpegWrapper;
    }

    /// <summary>
    /// Generates waveform data from an audio file.
    /// </summary>
    /// <param name="inputFile">The path to the input audio file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an array of floats representing the waveform data.</returns>
    public async Task<float[]> GenerateWaveformAsync(string inputFile, CancellationToken cancellationToken)
    {
        var mediaInfo = await _ffmpegWrapper.GetMediaInfo(inputFile, cancellationToken);

        var stream = mediaInfo.AudioStreams.FirstOrDefault();
        if (stream != null)
        {
            var outputFile = Path.Combine(Environment.CurrentDirectory, Names.AudioFiles, Path.GetRandomFileName());

            var conversion = _ffmpegWrapper.CreateConversion()
                .AddStream(stream)
                .AddParameter("-t 00:03:00")
                .AddParameter("-ac 1")
                .AddParameter("-ar 32000")
                .SetOutputFormat(Format.wav)
                .SetOutput(outputFile);

            await conversion.Start();

            //var result = new AudioReader(outputFile).GetData();
            await using var reader = new FileStream(outputFile, FileMode.Open);
            var result = await ReadAsFloat(reader);

            _ = Task.Run(() =>
            {
                TryDeleteFile(outputFile);
                TryDeleteFile(inputFile);
            });

            return result.ToArray();
        }

        return [];
    }

    /// <summary>
    /// Tries to delete a file at the specified path.
    /// </summary>
    /// <param name="filePath">The path to the file to delete.</param>
    private static void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static async Task<float[]> ReadAsFloat(Stream stream)
    {
        stream.Seek(44, SeekOrigin.Begin);

        int estimatedSamples = 7938000;
        float[] samples = ArrayPool<float>.Shared.Rent(estimatedSamples);
        int sampleCount = 0;

        var buffer = new byte[4096];
        int read;

        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            int limit = read - 2;
            for (int i = 0; i <= limit; i += 2)
            {
                samples[sampleCount++] = BitConverter.ToInt16(buffer, i) / 32768f;
            }
        }

        var result = new float[sampleCount];
        Array.Copy(samples, result, sampleCount);
        ArrayPool<float>.Shared.Return(samples);

        return result;
    }
}
