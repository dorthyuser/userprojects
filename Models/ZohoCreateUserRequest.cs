using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace synctesting1109.Models;

public sealed class ZohoCreateUserRequest
{
    [Required]
    [JsonPropertyName("users")]
    public List<ZohoCreateUserItem> Users { get; set; } = [];
}