using System.Text.Json.Serialization;

namespace TravelcardFunctionApp.Models;

public sealed class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string Details { get; set; } = string.Empty;
}