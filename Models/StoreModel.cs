using System.Text.Json.Serialization;

namespace hello_http_test.Models
{
    public class StoreModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
