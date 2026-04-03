namespace TravelcardService.Models
{
    public class ErrorResponse
    {
        public ErrorDetail Error { get; set; } = new ErrorDetail();
    }

    public class ErrorDetail
    {
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public int? BackendStatus { get; set; }
    }
}
