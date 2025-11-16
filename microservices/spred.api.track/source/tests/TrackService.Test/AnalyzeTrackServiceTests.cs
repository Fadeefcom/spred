using Moq;
using TrackService.Abstractions;
using TrackService.Components.Services;
using Xabe.FFmpeg;

namespace TrackService.Test;

public class AnalyzeTrackServiceTests
{
    private readonly Mock<IFFmpegWrapper> _ffmpegWrapperMock;
    private readonly Mock<IConversion> _conversionMock;
    private readonly AnalayzeTrackService _service;

    public AnalyzeTrackServiceTests()
    {
        _ffmpegWrapperMock = new Mock<IFFmpegWrapper>();
        _conversionMock = new Mock<IConversion>();

        _ffmpegWrapperMock
            .Setup(x => x.CreateConversion())
            .Returns(_conversionMock.Object);

        _conversionMock
            .Setup(x => x.AddParameter(It.IsAny<string>(), It.IsAny<ParameterPosition>()))
            .Returns(_conversionMock.Object);
        _conversionMock
            .Setup(x => x.AddParameter(It.IsAny<string>(), ParameterPosition.PostInput))
            .Returns(_conversionMock.Object);
        _conversionMock
            .Setup(x => x.SetOutput(It.IsAny<string>()))
            .Returns(_conversionMock.Object);
        _conversionMock
            .Setup(x => x.SetOutputFormat(It.IsAny<Format>()))
            .Returns(_conversionMock.Object);
        _conversionMock
            .Setup(x => x.SetOverwriteOutput(It.IsAny<bool>()))
            .Returns(_conversionMock.Object);
        _conversionMock
            .Setup(x => x.Start(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Mock.Of<IConversionResult>()));

        _service = new AnalayzeTrackService(_ffmpegWrapperMock.Object);
    }

    [Fact]
    public async Task Analyze_ShouldReturnMetadata_WhenValidPcmFile()
    {
        // Arrange
        var fakeFilePath = "input.wav";

        var originalAudio = new Mock<IAudioStream>();
        originalAudio.SetupGet(x => x.Codec).Returns("pcm_s16le");
        originalAudio.SetupGet(x => x.Bitrate).Returns(192);
        originalAudio.SetupGet(x => x.Channels).Returns(2);
        originalAudio.SetupGet(x => x.SampleRate).Returns(44100);
        originalAudio.SetupGet(x => x.Duration).Returns(TimeSpan.FromSeconds(120));

        var originalMediaInfo = new Mock<IMediaInfo>();
        originalMediaInfo.Setup(x => x.AudioStreams).Returns(new[] { originalAudio.Object });

        var finalAudio = new Mock<IAudioStream>();
        finalAudio.SetupGet(x => x.Codec).Returns("mp3");
        finalAudio.SetupGet(x => x.Bitrate).Returns(192);
        finalAudio.SetupGet(x => x.Channels).Returns(2);
        finalAudio.SetupGet(x => x.SampleRate).Returns(44100);
        finalAudio.SetupGet(x => x.Duration).Returns(TimeSpan.FromSeconds(120));

        var finalMediaInfo = new Mock<IMediaInfo>();
        finalMediaInfo.Setup(x => x.AudioStreams).Returns(new[] { finalAudio.Object });

        _ffmpegWrapperMock
            .Setup(x => x.GetMediaInfo(fakeFilePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(originalMediaInfo.Object);

        _ffmpegWrapperMock
            .Setup(x => x.GetMediaInfo(It.Is<string>(s => s != fakeFilePath), It.IsAny<CancellationToken>()))
            .ReturnsAsync(finalMediaInfo.Object);

        // Act
        var result = await _service.Analayze(fakeFilePath, CancellationToken.None);

        // Assert
        Assert.Equal((uint)192, result.Bitrate);
        Assert.Equal((uint)2, result.Channels);
        Assert.Equal((uint)44100, result.SampleRate);
        Assert.Equal("mp3", result.Codec);
        Assert.Equal(TimeSpan.FromSeconds(120), result.Duration);
    }
}