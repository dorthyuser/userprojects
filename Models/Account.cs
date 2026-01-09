using System;
using System.Text.Json.Serialization;

namespace AccountsFunction.Models
{
 public class Account
 {
 [JsonPropertyName("id")]
 public Guid Id { get; set; }

 [JsonPropertyName("name")]
 public string Name { get; set; } = string.Empty;

 [JsonPropertyName("email")]
 public string Email { get; set; } = string.Empty;

 [JsonPropertyName("address")]
 public string Address { get; set; } = string.Empty;
 }
}
