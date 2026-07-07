using System.Text.Json.Serialization;

namespace currency_calculator.Models;

public sealed class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;

    [JsonPropertyName("dependencies")]
    public HealthDependenciesResponse Dependencies { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
}