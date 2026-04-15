using System.Text.Json.Serialization;

namespace hello_http_test.Models
{
    /// <summary>
    /// Data transfer object for store payloads.
    /// </summary>
    public class StoreDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("domain")]
        public string Domain { get; set; }

        [JsonPropertyName("enabled")]
        public bool? Enabled { get; set; }
    }
}
