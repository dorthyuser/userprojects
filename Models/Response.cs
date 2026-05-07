using System.Text.Json.Serialization;

namespace Travelcardchsarplambda1050Lambda.Models;

public sealed class CreateTravelcardResponse
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = null!;

    [JsonPropertyName("token")]
    public string Token { get; set; } = null!;
}

public sealed class ErrorResponse
{
    public ErrorResponse(string code, string message)
    {
        Code = code;
        Message = message;
    }

    [JsonPropertyName("code")]
    public string Code { get; }

    [JsonPropertyName("message")]
    public string Message { get; }
}