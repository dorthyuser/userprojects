using System.Text.Json.Serialization;

namespace travelcardservice.Helpers
{
    public class ErrorResponse
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }
}
