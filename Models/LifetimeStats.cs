using System.Text.Json.Serialization;

namespace LifeTimeCalculator.Models;

public sealed class LifetimeStats
{
    [JsonPropertyName("totalYearsLived")]
    public double TotalYearsLived { get; set; }

    [JsonPropertyName("totalMonthsLived")]
    public double TotalMonthsLived { get; set; }

    [JsonPropertyName("totalWeeksLived")]
    public double TotalWeeksLived { get; set; }

    [JsonPropertyName("totalDaysLived")]
    public double TotalDaysLived { get; set; }

    [JsonPropertyName("totalHoursLived")]
    public double TotalHoursLived { get; set; }

    [JsonPropertyName("totalMinutesLived")]
    public double TotalMinutesLived { get; set; }

    [JsonPropertyName("totalSecondsLived")]
    public double TotalSecondsLived { get; set; }
}
