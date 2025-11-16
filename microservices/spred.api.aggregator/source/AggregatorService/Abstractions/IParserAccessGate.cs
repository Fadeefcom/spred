namespace AggregatorService.Abstractions;

/// <summary>
/// Interface for managing access to the parser endpoints,
/// typically used to block requests after exceeding a failure threshold.
/// </summary>
public interface IParserAccessGate
{
    /// <summary>
    /// Determines whether access to the parser is currently blocked.
    /// This is typically based on the number of failed access attempts.
    /// </summary>
    /// <returns><c>true</c> if access is blocked; otherwise, <c>false</c>.</returns>
    bool IsBlocked();

    /// <summary>
    /// Registers a failed access attempt.
    /// Should be called when an invalid access key is detected.
    /// </summary>
    void RegisterFailure();

    /// <summary>
    /// Resets the failure count and unblocks access.
    /// Useful for administrative or automated resets.
    /// </summary>
    void Reset();
}