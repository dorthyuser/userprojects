using System.Text.Json.Serialization;

namespace TravelCardFunctionApp.Models;

public class ErrorResponse
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
