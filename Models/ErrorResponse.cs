using System.Text.Json.Serialization;

namespace create_travelcard_prod.Models;

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public ErrorDetail Error { get; set; } = new();
}

public class ErrorDetail
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
