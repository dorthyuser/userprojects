using System.Text.Json.Serialization;

namespace LifeTimeCalculator.Models;

public sealed class AgeBreakdown
{
    [JsonPropertyName("years")]
    public int Years { get; set; }

    [JsonPropertyName("months")]
    public int Months { get; set; }

    [JsonPropertyName("weeks")]
    public int Weeks { get; set; }

    [JsonPropertyName("days")]
    public int Days { get; set; }

    [JsonPropertyName("hours")]
    public int Hours { get; set; }

    [JsonPropertyName("minutes")]
    public int Minutes { get; set; }

    [JsonPropertyName("seconds")]
    public int Seconds { get; set; }
}
