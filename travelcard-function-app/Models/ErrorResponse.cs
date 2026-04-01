namespace TravelcardFunctionApp.Models
{
    public class ErrorResponse
    {
        public ErrorDetail? Error { get; set; }
    }

    public class ErrorDetail
    {
        public string? Message { get; set; }
        public string? Detail { get; set; }
    }
}
