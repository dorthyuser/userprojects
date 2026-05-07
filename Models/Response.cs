using System.Text.Json.Serialization;

namespace Travelcardcharplambda1031Lambda.Models;

public sealed class CreateTravelcardResponse
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; init; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;
}

public sealed class ErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; init; }

    public ErrorResponse(string message)
    {
        Message = message;
    }
}