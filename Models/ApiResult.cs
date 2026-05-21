using System.Text.Json.Serialization;

namespace synctesting1050.Models;

public sealed class ApiResult
{
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("body")]
    public object? Body { get; set; }
}
