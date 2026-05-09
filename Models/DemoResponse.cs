using System.Text.Json.Serialization;

namespace new_project.Models;

public sealed class DemoResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}