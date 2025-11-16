using InferenceService.Abstractions;
using InferenceService.Helpers;
using Moq;
using Xabe.FFmpeg;

namespace InferenceService.Test;

public class WaveFormatHelperTests
{
    [Fact]
    public async Task GenerateWaveformAsync_ShouldReturnSamples_WhenAudioStreamExists()
    {
        var ffmpegMock = new Mock<IFFmpegWrapper>();
        var conversionMock = new Mock<IConversion>();
        var conversionResultMock = new Mock<IConversionResult>();

        var inputFile = Path.GetTempFileName();
        await File.WriteAllBytesAsync(inputFile, new byte[100]);

        string? generatedOutputFile = null;

        var audioStreamMock = new Mock<IAudioStream>();
        audioStreamMock.SetupGet(x => x.Index).Returns(0);

        var mediaInfoMock = new Mock<IMediaInfo>();
        mediaInfoMock.Setup(x => x.AudioStreams).Returns(new List<IAudioStream> { audioStreamMock.Object });

        ffmpegMock.Setup(x => x.GetMediaInfo(inputFile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaInfoMock.Object);

        conversionMock.Setup(x => x.AddStream(It.IsAny<IAudioStream>())).Returns(conversionMock.Object);
        conversionMock.Setup(x => x.AddParameter(It.IsAny<string>(), ParameterPosition.PostInput)).Returns(conversionMock.Object);
        conversionMock.Setup(x => x.SetOutputFormat(It.IsAny<Format>())).Returns(conversionMock.Object);
        conversionMock.Setup(x => x.SetOutput(It.IsAny<string>()))
            .Callback<string>(path =>
            {
                generatedOutputFile = path;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            })
            .Returns(conversionMock.Object);

        conversionMock.Setup(x => x.Start())
            .ReturnsAsync(() =>
            {
                if (generatedOutputFile == null)
                    throw new InvalidOperationException("Output file not set");
                File.WriteAllBytes(generatedOutputFile, CreateFakeWav(200));
                return conversionResultMock.Object;
            });

        ffmpegMock.Setup(x => x.CreateConversion()).Returns(conversionMock.Object);

        var helper = new WaveFormatHelper(ffmpegMock.Object);

        var result = await helper.GenerateWaveformAsync(inputFile, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, r => Assert.InRange(r, -1f, 1f));
    }

    [Fact]
    public async Task GenerateWaveformAsync_ShouldReturnEmpty_WhenNoAudioStream()
    {
        var ffmpegMock = new Mock<IFFmpegWrapper>();
        var mediaInfoMock = new Mock<IMediaInfo>();
        mediaInfoMock.Setup(x => x.AudioStreams).Returns(new List<IAudioStream>());

        ffmpegMock.Setup(x => x.GetMediaInfo(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaInfoMock.Object);

        var helper = new WaveFormatHelper(ffmpegMock.Object);

        var inputFile = Path.GetTempFileName();
        var result = await helper.GenerateWaveformAsync(inputFile, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    private static byte[] CreateFakeWav(int bytes)
    {
        var header = new byte[44]; // минимальный WAV-заголовок
        var data = new byte[bytes];
        var rnd = new Random(42);
        rnd.NextBytes(data);

        var full = new byte[header.Length + data.Length];
        Buffer.BlockCopy(header, 0, full, 0, header.Length);
        Buffer.BlockCopy(data, 0, full, header.Length, data.Length);
        return full;
    }
}
