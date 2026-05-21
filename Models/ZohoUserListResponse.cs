using System.Text.Json.Serialization;

namespace synctesting1050.Models;

public sealed class ZohoUserListResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("info")]
    public ZohoInfoDto? Info { get; set; }

    [JsonPropertyName("users")]
    public List<ZohoUserDto>? Users { get; set; }
}
