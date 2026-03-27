using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InitiateSwiftPayment.Models
{
    public class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public List<string> Details { get; set; } = new List<string>();
    }
}
