using System.Text.Json.Serialization;

namespace Travelcardlambdacsharp1133Lambda.Models;

public sealed class CreateTravelcardResponse
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}

public sealed class ErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; }

    public ErrorResponse(string message)
    {
        Message = message;
    }
}
