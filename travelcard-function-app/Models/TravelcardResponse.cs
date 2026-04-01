namespace TravelcardFunctionApp.Models
{
    public class TravelcardResponse
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public object? Data { get; set; }
    }
}
