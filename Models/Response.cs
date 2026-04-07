using System.Text.Json.Serialization;

namespace TcLambdaLambda.Models
{
    public class Response
    {
        public Response()
        {
            Status = string.Empty;
            Message = string.Empty;
            Details = null;
            Data = null;
        }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("details")]
        public string? Details { get; set; }

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }
}
