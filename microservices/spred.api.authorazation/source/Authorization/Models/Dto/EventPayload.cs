namespace Authorization.Models.Dto;

/// <summary>
/// Event payload class.
/// </summary>
public class EventPayload
{
    /// <summary>
    /// The type of the event.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// The payload data associated with the event.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = [];

    /// <summary>
    /// The timestamp of when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

