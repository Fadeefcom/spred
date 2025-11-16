using System.Diagnostics.CodeAnalysis;
using TrackService.Abstractions;
using Xabe.FFmpeg;

namespace TrackService.Components.Services;

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