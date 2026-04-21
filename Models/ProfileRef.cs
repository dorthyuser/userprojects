using System.Text.Json.Serialization;

namespace zoho_project_csharp.Models
{
    public class ProfileRef
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
