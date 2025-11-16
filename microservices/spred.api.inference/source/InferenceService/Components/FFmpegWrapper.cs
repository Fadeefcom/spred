using System.Diagnostics.CodeAnalysis;
using InferenceService.Abstractions;
using Xabe.FFmpeg;

namespace InferenceService.Components;

/// <inheritdoc/>
[ExcludeFromCodeCoverage]
public class FFmpegWrapper : IFFmpegWrapper
{
    /// <inheritdoc/>
    public Task<IMediaInfo> GetMediaInfo(string filePath, CancellationToken cancellationToken)
        => FFmpeg.GetMediaInfo(filePath, cancellationToken);
    
    /// <inheritdoc/>
    public IConversion CreateConversion()
        =>  FFmpeg.Conversions.New();
}