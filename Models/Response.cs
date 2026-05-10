using System.Text.Json.Serialization;

namespace Dashboard2Lambda.Models;

public sealed class Response
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("generated_id")]
    public string? GeneratedId { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    public static Response Ok(string generatedId) => new() { Success = true, GeneratedId = generatedId };
    public static Response Fail(string message) => new() { Success = false, ErrorMessage = message };
}
