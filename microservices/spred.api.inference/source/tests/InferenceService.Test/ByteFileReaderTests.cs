using System.Text;
using InferenceService.Helpers;
using InferenceService.Models;
using Microsoft.AspNetCore.Http;
using Moq;

namespace InferenceService.Test;

public class ByteFileReaderTests
{
    [Fact]
    public async Task GetFileBytesAsync_ReturnsCorrectBytes()
    {
        // Arrange
        var expected = Encoding.UTF8.GetBytes("test content");
        var stream = new MemoryStream(expected);
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
            .Returns<Stream, CancellationToken>((s, _) => stream.CopyToAsync(s));

        // Act
        var result = await ByteFileReader.GetFileBytesAsync(formFileMock.Object);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void OpenReadStreamFromBytes_ReturnsMemoryStream()
    {
        // Arrange
        var bytes = Encoding.UTF8.GetBytes("some data");

        // Act
        var stream = ByteFileReader.OpenReadStreamFromBytes(bytes);

        // Assert
        Assert.NotNull(stream);
        Assert.True(stream.CanRead);
        Assert.Equal(bytes, stream.ToArray());
    }

    [Fact]
    public void OpenReadStreamFromBytes_ThrowsOnNullOrEmpty()
    {
        Assert.Throws<ArgumentNullException>(() => ByteFileReader.OpenReadStreamFromBytes(null!));
        Assert.Throws<ArgumentNullException>(() => ByteFileReader.OpenReadStreamFromBytes(Array.Empty<byte>()));
    }

    [Fact]
    public async Task SaveFile_FromFormFile_SavesToDisk()
    {
        var bytes = Encoding.UTF8.GetBytes("file content");
        var stream = new MemoryStream(bytes);
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
            .Returns<Stream, CancellationToken>((s, _) => stream.CopyToAsync(s));

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var audioDir = Path.Combine(tempDir, Names.AudioFiles);
        Directory.CreateDirectory(audioDir);

        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = tempDir;

        try
        {
            var path = await ByteFileReader.SaveFile(formFileMock.Object, null);
            Assert.True(File.Exists(path));
            Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            Environment.CurrentDirectory = originalDir;
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SaveFile_FromStream_SavesToDisk()
    {
        var bytes = Encoding.UTF8.GetBytes("stream data");
        var stream = new MemoryStream(bytes);

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var audioDir = Path.Combine(tempDir, Names.AudioFiles);
        Directory.CreateDirectory(audioDir);

        var originalDir = Environment.CurrentDirectory;
        Environment.CurrentDirectory = tempDir;

        try
        {
            var path = await ByteFileReader.SaveFile(stream, null);
            Assert.True(File.Exists(path));
            Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            Environment.CurrentDirectory = originalDir;
            Directory.Delete(tempDir, true);
        }
    }
}