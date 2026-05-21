using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace synctesting1050.Models;

public sealed class CreateUserRequest
{
    [JsonPropertyName("users")]
    [Required]
    [MinLength(1)]
    [MaxLength(1)]
    public List<CreateUserItem> Users { get; set; } = [];
}
