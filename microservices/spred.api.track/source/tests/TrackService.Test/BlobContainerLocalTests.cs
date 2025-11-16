using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TrackService.Components.Services;
using TrackService.Configuration;

namespace TrackService.Test;

public class BlobContainerLocalTests
{
    private readonly string _testDir;
    private readonly BlobContainerLocal _service;

    public BlobContainerLocalTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        var options = Options.Create(new BlobOptions
        {
            ContainerName = Path.GetFileName(_testDir),
            BlobConnectString = "test"
        });

        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
                         .Returns(Mock.Of<ILogger<BlobContainerLocal>>());

        _service = new BlobContainerLocal(options, loggerFactoryMock.Object);
    }

    [Fact]
    public async Task UploadFile_ShouldWriteStreamToFile()
    {
        // Arrange
        var trackId = Guid.NewGuid();
        var content = "test content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var (container, name) = await _service.UploadFile(stream, trackId, CancellationToken.None);

        // Assert
        var filePath = Path.Combine(Environment.CurrentDirectory, container, name);
        Assert.True(File.Exists(filePath));
        var readContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(content, readContent);
    }

    [Fact]
    public async Task CheckIfExists_ShouldReturnTrue_IfFileExists()
    {
        // Arrange
        var trackId = Guid.NewGuid();
        var filePath = Path.Combine(Environment.CurrentDirectory, Path.GetFileName(_testDir), trackId.ToString());
        await File.WriteAllTextAsync(filePath, "exists");

        // Act
        var result = await _service.CheckIfExists(trackId, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckIfExists_ShouldReturnFalse_IfFileMissing()
    {
        var result = await _service.CheckIfExists(Guid.NewGuid(), CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteIfExists_ShouldDeleteFile_IfExists()
    {
        // Arrange
        var trackId = Guid.NewGuid();
        var filePath = Path.Combine(Environment.CurrentDirectory, Path.GetFileName(_testDir), trackId.ToString());
        await File.WriteAllTextAsync(filePath, "to be deleted");

        // Act
        await _service.DeleteIfExists(trackId, CancellationToken.None);

        // Assert
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteIfExists_ShouldDoNothing_IfFileNotExists()
    {
        var trackId = Guid.NewGuid();
        var filePath = Path.Combine(Environment.CurrentDirectory, Path.GetFileName(_testDir), trackId.ToString());

        // Should not throw
        await _service.DeleteIfExists(trackId, CancellationToken.None);

        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task GetFile_ShouldReturnReadableStream()
    {
        // Arrange
        var trackId = Guid.NewGuid();
        var expectedContent = "some data";
        var filePath = Path.Combine(Environment.CurrentDirectory, Path.GetFileName(_testDir), trackId.ToString());
        await File.WriteAllTextAsync(filePath, expectedContent);

        // Act
        var stream = await _service.GetFile(trackId, CancellationToken.None);
        using var reader = new StreamReader(stream);
        var actualContent = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal(expectedContent, actualContent);
    }
}