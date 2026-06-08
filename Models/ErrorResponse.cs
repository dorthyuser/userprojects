using System.Text.Json.Serialization;

namespace LifeTimeCalculator.Models;

public sealed class ErrorResponse
{
    public ErrorResponse(string error, string details)
    {
        Error = error;
        Details = details;
    }

    [JsonPropertyName("error")]
    public string Error { get; set; }

    [JsonPropertyName("details")]
    public string Details { get; set; }
}
