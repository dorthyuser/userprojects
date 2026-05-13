using System.Text.Json.Serialization;

namespace Travelcardcsharp1017Lambda.Models;

public sealed class CreateTravelcardResponse
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = null!;

    [JsonPropertyName("token")]
    public string Token { get; set; } = null!;
}

public sealed class ErrorResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    public ErrorResponse(string message)
    {
        Message = message;
    }
}