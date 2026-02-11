using System.Text.Json.Serialization;

namespace SalesforceAccountFunctions.Helpers.Logging
{
    public class ErrorResponse
    {
        [JsonPropertyName("error")]
        public ErrorDetail Error { get; set; } = new ErrorDetail();
    }

    public class ErrorDetail
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public string Details { get; set; } = string.Empty;
    }
}
