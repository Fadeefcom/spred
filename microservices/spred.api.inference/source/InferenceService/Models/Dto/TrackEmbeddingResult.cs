using System.Text.Json.Serialization;

namespace InferenceService.Models.Dto;

public sealed record TrackEmbeddingResult
{
    [JsonPropertyName("TrackId")]
    public Guid TrackId { get; init; }

    [JsonPropertyName("SpredUserId")]
    public Guid SpredUserId { get; init; }

    [JsonPropertyName("Embedding")]
    public float[] Embedding { get; init; } = [];

    [JsonPropertyName("EmbeddingShape")]
    public int[] EmbeddingShape { get; init; } = [];

    [JsonPropertyName("DeviceUsed")]
    public string DeviceUsed { get; init; } = string.Empty;

    [JsonPropertyName("ModelPath")]
    public string ModelPath { get; init; } = string.Empty;

    [JsonPropertyName("ModelVersion")]
    public string ModelVersion { get; init; } = string.Empty;

    [JsonPropertyName("Success")]
    public bool Success { get; init; }

    [JsonPropertyName("ErrorMessage")]
    public string? ErrorMessage { get; init; }
}