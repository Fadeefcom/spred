using System.Text.Json;
using Spred.Bus.Contracts;

namespace TrackService.Test;

public class InferenceRequestTests
{
    [Fact]
    public void Should_Initialize_With_Valid_Values()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var trackId = Guid.NewGuid();

        // Act
        var request = new InferenceRequest
        {
            SpredUserId = userId,
            TrackId = trackId
        };

        // Assert
        Assert.Equal(userId, request.SpredUserId);
        Assert.Equal(trackId, request.TrackId);
    }

    [Fact]
    public void Should_Serialize_And_Deserialize_To_Json()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        var request = new InferenceRequest
        {
            SpredUserId = userId,
            TrackId = trackId
        };

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<InferenceRequest>(json);

        // Assert
        Assert.Equal(userId, deserialized!.SpredUserId);
        Assert.Equal(trackId, deserialized.TrackId);
    }
}