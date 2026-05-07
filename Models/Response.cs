using System.Text.Json.Serialization;

namespace Travelcardcharplambda225Lambda.Models;

public sealed class Response
{
    [JsonPropertyName("travelcardId")]
    public string TravelcardId { get; set; } = null!;

    [JsonPropertyName("token")]
    public string Token { get; set; } = null!;
}

public sealed class ErrorResponse
{
    [JsonPropertyName("field")]
    public string Field { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    public ErrorResponse(string field, string message)
    {
        Field = field;
        Message = message;
    }
}