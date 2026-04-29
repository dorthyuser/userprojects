namespace Models
{
    public class ErrorResponse
    {
        public string Error { get; set; }
        public string Details { get; set; }

        public ErrorResponse(string error, string details)
        {
            Error = error;
            Details = details;
        }
    }
}