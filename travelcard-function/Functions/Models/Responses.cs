namespace TravelcardFunction.Models
{
    public class TravelcardCreateResponse
    {
        public string TravelcardId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
    }
}
