using System.Text.Json;
using System.Text.Json.Serialization;

namespace TcTesting9Lambda.Models
{
    public class Response
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("data")]
        public JsonElement? Data { get; set; }

        [JsonPropertyName("status_code")]
        public int? StatusCode { get; set; }

        public Response()
        {
            Success = false;
            Message = null;
            Error = null;
            Data = null;
            StatusCode = null;
        }
    }
}
