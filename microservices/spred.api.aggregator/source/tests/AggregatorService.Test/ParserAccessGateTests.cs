using AggregatorService.Components;

namespace AggregatorService.Test;

public class ParserAccessGateTests
{
    [Fact]
    public void IsBlocked_ShouldReturnFalse_WhenAttemptsAreBelowLimit()
    {
        // Arrange
        var gate = new ParserAccessGate();

        // Act
        var result = gate.IsBlocked();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsBlocked_ShouldReturnTrue_WhenAttemptsReachLimit()
    {
        // Arrange
        var gate = new ParserAccessGate();

        // Act
        for (int i = 0; i < 5; i++)
            gate.RegisterFailure();

        // Assert
        Assert.True(gate.IsBlocked());
    }

    [Fact]
    public void IsBlocked_ShouldReturnFalse_AfterReset()
    {
        // Arrange
        var gate = new ParserAccessGate();
        for (int i = 0; i < 5; i++)
            gate.RegisterFailure();
        gate.Reset();

        // Act
        var result = gate.IsBlocked();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RegisterFailure_ShouldIncrementAttempts()
    {
        // Arrange
        var gate = new ParserAccessGate();

        // Act
        for (int i = 0; i < 4; i++)
            gate.RegisterFailure();

        // Assert
        Assert.False(gate.IsBlocked());

        gate.RegisterFailure();
        Assert.True(gate.IsBlocked());
    }
}