using System.Text.Json.Serialization;

namespace synctesting1050.Models;

public sealed class ZohoUserResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("user")]
    public ZohoUserDto? User { get; set; }
}
