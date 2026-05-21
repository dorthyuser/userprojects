using System.Text.Json.Serialization;

namespace synctesting1109.Models;

public sealed class LocalUserResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("user")]
    public LocalUserDto? User { get; set; }
}