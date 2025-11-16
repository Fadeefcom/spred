using AggregatorService.Models;
using FluentAssertions;

namespace AggregatorService.Test;

public class TrackInferenceTests
{
    [Fact]
    public void Should_Create_TrackInference_With_Partial_Data()
    {
        // Arrange
        var trackId = Guid.NewGuid();
        var genre = "electronic";

        // Act
        var inference = new TrackInference
        {
            TrackId = trackId,
            Genre = genre,
            TrackOwner = Guid.NewGuid()
        };

        // Assert
        inference.TrackId.Should().Be(trackId);
        inference.Genre.Should().Be(genre);
        inference.TrackOwner.Should().NotBeEmpty();
    }
}