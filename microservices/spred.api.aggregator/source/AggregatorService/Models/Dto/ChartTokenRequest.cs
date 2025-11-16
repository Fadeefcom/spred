using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace AggregatorService.Models.Dto;

public record ChartTokenRequest
{
    [JsonPropertyName("refreshtoken")]
    [JsonProperty("refreshtoken")]
    public required string RefreshToken { get; init; }
}