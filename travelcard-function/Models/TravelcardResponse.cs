namespace TravelcardFunction.Models
{
    public class TravelcardResponse
    {
        public string TravelcardId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string? Details { get; set; } = null;
    }
}
