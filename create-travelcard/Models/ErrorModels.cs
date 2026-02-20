using System.Text.Json.Serialization;

namespace CreateTravelcard.Models
{
    public class ErrorItem
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("field")]
        public string Field { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class ErrorResponse
    {
        [JsonPropertyName("correlationId")]
        public string CorrelationId { get; set; }

        [JsonPropertyName("errors")]
        public ErrorItem[] Errors { get; set; }
    }
}
