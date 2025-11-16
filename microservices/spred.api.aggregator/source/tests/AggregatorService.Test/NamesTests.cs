using AggregatorService.Configurations;

namespace AggregatorService.Test;

public class NamesTests
{
    [Fact]
    public void VideoFiles_Should_Return_Correct_Value()
    {
        // Act
        var result = Names.VideoFiles;

        // Assert
        Assert.Equal("VideoFiles", result);
    }

    [Fact]
    public void AudioFiles_Should_Return_Correct_Value()
    {
        // Act
        var result = Names.AudioFiles;

        // Assert
        Assert.Equal("AudioFiles", result);
    }
}