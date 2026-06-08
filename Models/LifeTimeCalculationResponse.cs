using System.Text.Json.Serialization;

namespace LifeTimeCalculator.Models;

public sealed class LifeTimeCalculationResponse
{
    [JsonPropertyName("dateOfBirth")]
    public string DateOfBirth { get; set; } = string.Empty;

    [JsonPropertyName("currentDate")]
    public string CurrentDate { get; set; } = string.Empty;

    [JsonPropertyName("age")]
    public AgeBreakdown Age { get; set; } = new();

    [JsonPropertyName("lifetimeStats")]
    public LifetimeStats LifetimeStats { get; set; } = new();
}
