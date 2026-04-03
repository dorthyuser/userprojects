namespace travelcard_service.Models
{
    public class ErrorResponse
    {
        public string? Message { get; set; }
        public int StatusCode { get; set; }
        public object? Details { get; set; }
    }
}
