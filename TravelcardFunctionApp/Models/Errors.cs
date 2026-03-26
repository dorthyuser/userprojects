namespace TravelcardFunctionApp.Models
{
    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string? Details { get; set; }
    }

    public class CreateResponse
    {
        public string TravelcardId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
